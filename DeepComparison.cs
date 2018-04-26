public static class DeepComparison<T>
{
    /// <summary>
    /// Deep comparison of complex objects.
    /// Returns true if same.
    /// </summary>
    /// <param name="obj1"></param>
    /// <param name="obj2"></param>
    /// <param name="nullEqualsEmpty">Whether null and empty object will be treated as equal or not. If true then those states will be treated as equal: null | empty | default | count 0 list</param>
    /// <param name="depth">How deep the compare method will go. -1 (or any negative): infinite level; 0 (zer0): only immediate, non-complex properties, >0 (any positive number): comparison will continue till the mentioned level</param>
    /// <param name="mismatchInfo">[For debugging purpose] Pass an empty ExpandoObject; information about mismatch will be included.</param>
    /// <returns></returns>
    public static bool CompareObject(T obj1, T obj2, bool nullEqualsEmpty = true, int depth = -1, dynamic mismatchInfo = null)
    {
        if (mismatchInfo == null)
            mismatchInfo = new ExpandoObject();

        if (obj1 == null && obj2 == null) // if both objects are null, return true
            return true;
        else if (!nullEqualsEmpty && (obj1 == null || obj2 == null)) // if null & empty are different and if any obj is null, return false
            return false;

        Type type = typeof(T);
        var properties = type.GetProperties().Where(x=>x.DeclaringType != new Models.ModelBase().GetType()); // ModelBase properties are excluded; since validation errors etc can contain in ModelBase which should not be detected as user changes
        // start traversing all properties
        foreach (PropertyInfo propInfo in properties)
        {
            object val1 = null;
            object val2 = null;
            if (obj1 != null)
            {
                // some properties like Stream.ReadTimeout may already in execption state
                try
                {
                    val1 = propInfo.GetValue(obj1, null);
                } catch { }
            }
            if (obj2 != null)
            {
                try
                {
                    val2 = propInfo.GetValue(obj2, null);
                } catch { }
            }

            var compValx = (val1 as IComparable) != null ? val1 as IComparable : val2 as IComparable; // take the not null value to create iComparable obj

            if (propInfo.Name.Equals("HasErrors"))
                continue;

            // first check for null
            if (val1 == null && val2 == null)
                continue;
            else if (compValx != null) // ** if directly comparable (valueType/string), no need to go further, check inside
            {
                if (nullEqualsEmpty)     // if nullEqualsEmpty=true then initialize null property with empty value
                {
                    if (val1 == null)
                        val1 = propInfo.PropertyType == typeof(string) ? string.Empty : Activator.CreateInstance(Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType);
                    else if (val2 == null)
                        val2 = propInfo.PropertyType == typeof(string) ? string.Empty : Activator.CreateInstance(Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType);
                }
                if (!IComparable.Equals(val1, val2))
                {
                    mismatchInfo.info = propInfo.Name + ": " + (val1 == null ? "{null}" : val1) + " / " + (val2 == null ? "{null}" : val2);
                    return false;
                }
            }
            else if (!nullEqualsEmpty && (val1 == null || val2 == null)) // if not comparable object and one is null, return false
            {
                mismatchInfo.info = propInfo.Name + ": " + val1 + " / " + val2;
                return false;
            }
            // Collection type property - can be complex or primitive. Carefull: 'string' also implemented IEnumerable<char>, but string properties won't get through this far.
            else if (propInfo.PropertyType.GetInterfaces().Any(o => o == typeof(IEnumerable)))
            {
                var list1 = (IEnumerable<dynamic>)val1;
                var list2 = (IEnumerable<dynamic>)val2;
                // try to evaluate the count of lists

                if (list1 == null || list2 == null) // if one list is null, no need to go further
                {
                    bool result = true;
                    // if both null or empty
                    if ((list1 == null || list1.Count() == 0) && (list2 == null || list2.Count() == 0))
                    {
                        if (nullEqualsEmpty) continue;
                        else result = false;
                    }
                    // if one is null and another has items
                    else if ((list1 == null && list2.Count() > 0) || (list2 == null && list1.Count() > 0))
                        result = false;

                    mismatchInfo.info = propInfo.Name + ": unequal length. List1 count: " + (list1 == null ? "{null}" : list1.Count().ToString()) + " / List2 count: " + (list2 == null ? "{null}" : list2.Count().ToString());
                    return result;
                }
                else if (list1.Count() != list2.Count())
                {
                    mismatchInfo.info = propInfo.Name + ": unequal length. List1 count: " + list1.Count() + " / List2 count: " + list2.Count();
                    return false;
                }
                else if ((list1.FirstOrDefault() as IComparable) != null)   // apply a trim; if lists of comparable variables, use LINQ.SequenceEqual
                {
                    if (list1.SequenceEqual(list2) == false)
                    {
                        mismatchInfo.info = propInfo.Name + ": comparable list, different value found.";
                        return false;
                    }
                    else
                        continue;
                }
                // lists of complex objects; use enumerator to traverse entire lists
                else
                {
                    var enumerator1 = Enumerable.Cast<dynamic>(val1 as dynamic).GetEnumerator();
                    var enumerator2 = Enumerable.Cast<dynamic>(val2 as dynamic).GetEnumerator();
                    while (enumerator1.MoveNext() && enumerator2.MoveNext())
                    {
                        Type itemType = null;
                        if (enumerator1.Current != null)
                            itemType = enumerator1.Current.GetType();
                        else if (val1.GetType().GetGenericArguments() != null && val1.GetType().GetGenericArguments().Count() == 1)  // type.GetGenericArguments (i.e. List<Type_agr_1, Type_agr_2,...>) to get type
                            itemType = val1.GetType().GetGenericArguments().Single();

                        if (itemType != null &&
                            depth != 0 &&
                            InvokeCompare(itemType, enumerator1.Current, enumerator2.Current, nullEqualsEmpty, depth, mismatchInfo) == false)
                            return false;   // unequal value found in lists property
                    }
                }
            }
            else    // scaler complex property
            {
                // as per previous check, both properties are not null. so, if one is null, pass an instantiated object for it
                object tempObj1, tempObj2;
                tempObj1 = nullEqualsEmpty && val1 != null ? val1 : Activator.CreateInstance(propInfo.PropertyType);
                tempObj2 = nullEqualsEmpty && val2 != null ? val2 : Activator.CreateInstance(propInfo.PropertyType);
                if (depth != 0 && InvokeCompare(propInfo.PropertyType, tempObj1, tempObj2, nullEqualsEmpty, depth, mismatchInfo) == false)
                    return false;   // unequal property found
            }
        }
        return true; // no mismatch found
    }

    /// <summary>
    /// Reflection used to initiate ObjectHelper<> and invoked the Compare method afterwords
    /// </summary>
    /// <param name="t">The type, for which the ObjectHelper<> will be initiated.</param>
    /// <param name="obj1">1st Value to pass to Comapare method.</param>
    /// <param name="obj2">2nd Value to pass to Comapare method.</param>
    /// <returns></returns>
    static bool InvokeCompare(Type t, object obj1, object obj2, bool nullEqualsEmpty, int depth, dynamic mismatched)
    {
        Type objectHelper = typeof(ObjectHelper<>).MakeGenericType(new Type[] { t });
        MethodInfo theMethod = objectHelper.GetMethod("CompareObject", BindingFlags.Public | BindingFlags.Static);
        var parameters = new object[] { obj1, obj2, nullEqualsEmpty, depth - 1, mismatched };
        var result = theMethod.Invoke(null, parameters);

        return (bool)result;
    }
}
