using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DeepComparison
{
    public static class DeepComparison
    {
        /// <summary>
        /// Deep comparison of complex objects.
        /// Returns False any difference found.
        /// </summary>
        /// <param name="obj1">Object A</param>
        /// <param name="obj2">Object B</param>
        /// <param name="treatNullAndEmptyAsSame">Whether null and empty object will be treated as equal or not. If true then those states will be treated as equal: null | empty | default | count 0 list</param>
        /// <param name="depth">How deep the compare method will go. -1 (or any negative): infinite level; 0 (zer0): only immediate, non-complex properties, >0 (any positive number): comparison will continue till the mentioned level</param>
        /// <param name="excludeType">If any Type of object should be excluded from check, pass the Type</param>
        /// <param name="mismatchInfo">[For debugging purpose] Pass an empty dictionary; information about mismatch will be included in that pattern: { PropertyName: [ valueA, valueB, "{Info}" ] }.</param>
        /// <returns></returns>
        public static bool Compare<T>(T obj1, T obj2, bool treatNullAndEmptyAsSame = true, int depth = -1, Type excludeType = null, IDictionary<string, object[]> mismatchInfo = null) where T : class
        {
            if (mismatchInfo == null)
                mismatchInfo = new Dictionary<string, object[]>();

            if (obj1 is null && obj2 is null) // if both objects are null, return true
                return true;
            else if (!treatNullAndEmptyAsSame && (obj1 is null || obj2 is null)) // if null & empty are different and if any obj is null, return false
                return false;

            Type type = typeof(T);
            
            var properties = type.GetProperties().Where(x => excludeType is null || x.DeclaringType != excludeType);

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
                    }
                    catch { }
                }
                if (obj2 != null)
                {
                    try
                    {
                        val2 = propInfo.GetValue(obj2, null);
                    }
                    catch { }
                }

                var compValx = (val1 as IComparable) != null ? val1 as IComparable : val2 as IComparable; // take the not null value to create iComparable obj

                if (propInfo.Name.Equals("HasErrors"))
                    continue;

                // first check for null
                if (val1 == null && val2 == null)
                    continue;
                else if (compValx != null) // ** if directly comparable (valueType/string), no need to go further, check inside
                {
                    if (treatNullAndEmptyAsSame)     // if nullEqualsEmpty=true then initialize null property with empty value
                    {
                        if (val1 == null)
                            val1 = propInfo.PropertyType == typeof(string) ? string.Empty : Activator.CreateInstance(Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType);
                        else if (val2 == null)
                            val2 = propInfo.PropertyType == typeof(string) ? string.Empty : Activator.CreateInstance(Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType);
                    }
                    if (!IComparable.Equals(val1, val2))
                    {
                        mismatchInfo.Add(propInfo.Name, new object[] { val1, val2, "Unequal" });
                        return false;
                    }
                }
                else if (!treatNullAndEmptyAsSame && (val1 == null || val2 == null)) // if not comparable object and one is null, return false
                {
                    mismatchInfo.Add(propInfo.Name, new object[] { val1, val2, "Unequal" });
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
                            if (treatNullAndEmptyAsSame) continue;
                            else result = false;
                        }
                        // if one is null and another has items
                        else if ((list1 == null && list2.Count() > 0) || (list2 == null && list1.Count() > 0))
                        {
                            result = false;
                        }

                        mismatchInfo.Add(propInfo.Name, new object[]
                            {
                                list1 == null ? (long?)null : list1.LongCount(),
                                list2 == null ? (long?)null : list2.LongCount(),
                                "Collection unequal in length."
                            });
                        return result;
                    }
                    else if (list1.Count() != list2.Count())
                    {
                        mismatchInfo.Add(propInfo.Name, new object[] { list1.LongCount(), list2.Count(), "Collection unequal in length." });
                        return false;
                    }
                    else if ((list1.FirstOrDefault() as IComparable) != null)   // apply a trim; if lists of comparable variables, use LINQ.SequenceEqual
                    {
                        if (list1.SequenceEqual(list2) == false)
                        {
                            mismatchInfo.Add(propInfo.Name, new object[] { null, null, "Different value found in collection." });
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
                                InvokeCompare(itemType, enumerator1.Current, enumerator2.Current, treatNullAndEmptyAsSame, depth, excludeType, mismatchInfo) == false)
                                return false;   // unequal value found in lists property
                        }
                    }
                }
                else    // scaler complex property
                {
                    // as per previous check, both properties are not null. so, if one is null, pass an instantiated object for it
                    object tempObj1, tempObj2;
                    tempObj1 = treatNullAndEmptyAsSame && val1 != null ? val1 : Activator.CreateInstance(propInfo.PropertyType);
                    tempObj2 = treatNullAndEmptyAsSame && val2 != null ? val2 : Activator.CreateInstance(propInfo.PropertyType);
                    if (depth != 0 && InvokeCompare(propInfo.PropertyType, tempObj1, tempObj2, treatNullAndEmptyAsSame, depth, excludeType, mismatchInfo) == false)
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
        static bool InvokeCompare(Type t, object obj1, object obj2, bool nullEqualsEmpty, int depth, Type exclude, dynamic mismatched)
        {
            Type deepComparisonType = typeof(DeepComparison<>).MakeGenericType(new Type[] { t });
            MethodInfo theMethod = deepComparisonType.GetMethod(nameof(Compare), BindingFlags.Public | BindingFlags.Static);
            var parameters = new object[] { obj1, obj2, nullEqualsEmpty, depth - 1, exclude, mismatched };
            var result = theMethod.Invoke(null, parameters);

            return (bool)result;
        }
    }

}
