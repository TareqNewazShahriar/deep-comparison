# Deep Comparison of Objects
This is helper method, compares two objects - property by property; checks the values; if any difference is found, it will inform it. It will traverse the object deeply up to n-th level and comprares promitive types, collections/enumrables, associated complex objects using recursion.

### Method definition
````c#
bool CompareObject(T obj1, T obj2, bool nullEqualsEmpty = true, int depth = -1, dynamic mismatchInfo = null);
````

### Parameter description
* **obj1 & obj2** (complext type): the objects that will be compared.
* **nullEqualsEmpty** (boolean): Whether null and empty values will be treated as equal or not. If true then those states will be treated as equal: null | empty | default | count 0 list.
* **depth** (int): How deep the compare method will go. -1 (or any negative): infinite level; 0 (zer0): only immediate, non-complex properties, >0 (any positive number): comparison will continue till the mentioned level.
* **mismatchInfo** (ExpandoObject): [For debugging purpose] Pass an empty ExpandoObject; information about mismatch will be included.

*Returns* (boolean): Whether the values of obj1 and obj2 are same or not.

### Sample call
````c#
System.Dynamic.ExpandoObject mismatchInfo;
bool identical = CompareObject<Product>(currentProduct, copyProduct, nullEqualsEmpty: true, mismatchInfo: mismatchInfo);

if (!identical)
{
  MessageBox.Show("You have unsaved changes. Do you want to save your changes?", "Confirmation", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
}
````
