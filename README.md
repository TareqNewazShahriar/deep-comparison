# Deep Comparison of Objects
Takes two instances of any complex type and compares deeply up to n-th level.

Method definition with parameter descriptions
````c#
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
bool CompareObject(T obj1, T obj2, bool nullEqualsEmpty = true, int depth = -1, dynamic mismatchInfo = null);
````

Sample call:
````c#
System.Dynamic.ExpandoObject mismatchInfo;
bool identical = CompareObject<Product>(oldObj, currentObj, nullEqualsEmpty: true, mismatchInfo: mismatchInfo);

if (identical = false)
{
  MessageBox.Show("You have unsaved changes. Do you want to save your changes", "Confirmation", MessageBoxIcon.Warning, MessageBoxButtons.YesNoCancel);
}
````
