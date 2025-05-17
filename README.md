# Deep Comparison of Objects

### *[deprecated]*

This is a .Net Nuget package to compare two objects - property by property; checks the values; It will inform any difference is found or not. It will traverse the object deeply up to n-th level and comprares primitive types, collections/enumrables, associated complex objects using recursion.

Test Nuget Package: [DeepComparison](https://www.nuget.org/packages/Tns.DeepComparison/1.0.0#show-readme-container)

### Accuracy
This comparer had been used regularly by a team of software engineers for more than a year. So many bugs are fixed in that time. After a period of time like 7/8 months, this method became stable (I hope!).
 
### Method definition
````c#
bool CompareObject(T obj1, T obj2, bool treatNullAndEmptyAsSame = true, int depth = -1, Type excludeType, dynamic mismatchInfo = null)
````

### Parameter description
* **obj1 & obj2** (complex type): the objects that will be compared.
* **treatNullAndEmptyAsSame** (boolean): Whether null and empty values will be treated as equal or not. If true then those states will be treated as equal: null | empty | default | count 0 list.
* **depth** (int): How deep the compare method will go. -1 (or any negative): infinite level; 0 (zer0): only immediate, non-complex properties, >0 (any positive number): comparison will continue till the mentioned level.

* **excludeType** (Type): Pass the *Type* of the object that you want to exclude from the comparison.

* **mismatchInfo** (ExpandoObject): [For debugging purpose] Pass an empty ExpandoObject; information about mismatch will be included.

*Returns* (boolean): Whether the values of obj1 and obj2 are same or not.

### Sample call
````c#
System.Dynamic.ExpandoObject mismatchInfo;
bool isIdentical = DeepComparison.CompareObject(currentProduct, oldProduct, nullEqualsEmpty: true, excludeType: typeof(ProductCategory), mismatchInfo: mismatchInfo);

if (!isIdentical)
{
  MessageBox.Show("You have unsaved changes. Do you want to save your changes?", "Confirmation", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
}
````
