using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Ve dropdown de chon class cu the cho field/list [SerializeReference].
// Ap dung cho tung phan tu cua List<T> khi T la interface hoac abstract class -
// Unity se tu goi drawer nay cho moi element vi attribute nam tren field khai bao List.
[CustomPropertyDrawer(typeof(SerializeReferenceDropdownAttribute))]
public class SerializeReferenceDropdownDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ManagedReference)
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        // Nut dropdown hien ten type hien tai
        Rect dropdownRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        string currentTypeName = GetShortTypeName(property.managedReferenceFullTypename);
        string buttonLabel = string.IsNullOrEmpty(currentTypeName) ? "(Select effect type...)" : currentTypeName;

        if (EditorGUI.DropdownButton(dropdownRect, new GUIContent(buttonLabel), FocusType.Keyboard))
        {
            ShowTypeMenu(property);
        }

        // Ve field cua instance da chon (neu co) ngay ben duoi dropdown
        if (property.managedReferenceValue != null)
        {
            EditorGUI.indentLevel++;
            Rect fieldsRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2f,
                position.width, position.height - EditorGUIUtility.singleLineHeight - 2f);
            EditorGUI.PropertyField(fieldsRect, property, GUIContent.none, true);
            EditorGUI.indentLevel--;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight + 2f;
        if (property.propertyType == SerializedPropertyType.ManagedReference && property.managedReferenceValue != null)
        {
            height += EditorGUI.GetPropertyHeight(property, true);
        }
        return height;
    }

    private void ShowTypeMenu(SerializedProperty property)
    {
        Type interfaceType = GetElementType();
        if (interfaceType == null)
        {
            Debug.LogWarning("Khong xac dinh duoc type cua field de tim danh sach class ke thua.");
            return;
        }

        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("(None)"), false, () =>
        {
            property.serializedObject.Update();
            property.managedReferenceValue = null;
            property.serializedObject.ApplyModifiedProperties();
        });

        var candidateTypes = TypeCache.GetTypesDerivedFrom(interfaceType)
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .OrderBy(t => t.Name);

        foreach (var type in candidateTypes)
        {
            Type capturedType = type;
            menu.AddItem(new GUIContent(type.Name), false, () =>
            {
                property.serializedObject.Update();
                property.managedReferenceValue = Activator.CreateInstance(capturedType);
                property.serializedObject.ApplyModifiedProperties();
            });
        }

        menu.ShowAsContext();
    }

    // Dung reflection tren chinh field C# (fieldInfo co san trong PropertyDrawer) thay vi parse
    // chuoi managedReferenceFieldTypename - chuoi do bi gay voi generic interface (vi du
    // IEffect<IAttackable>) vi ben trong no co dau cach ("IEffect`1[[IAttackable, Assembly-CSharp]]"),
    // lam Split(' ') cat sai vi tri. Reflection thi luon dung du field la interface, generic, hay array.
    private Type GetElementType()
    {
        Type fieldType = fieldInfo.FieldType;

        if (fieldType.IsArray)
        {
            return fieldType.GetElementType();
        }

        if (fieldType.IsGenericType)
        {
            // Ap dung cho List<T> va cac generic collection tuong tu - lay T
            var genericArgs = fieldType.GetGenericArguments();
            if (genericArgs.Length == 1)
            {
                return genericArgs[0];
            }
        }

        // Field khong phai collection (vi du 1 field IEffect<IAttackable> don, khong nam trong List)
        return fieldType;
    }

    private string GetShortTypeName(string fullTypename)
    {
        if (string.IsNullOrEmpty(fullTypename))
            return null;

        // Chi tach o dau cach DAU TIEN - phan con lai (className) co the chua dau cach ben trong
        // neu la generic type, giu nguyen thay vi Split(' ') thong thuong de tranh cat sai.
        string[] parts = fullTypename.Split(new[] { ' ' }, 2);
        string className = parts.Length > 1 ? parts[1] : fullTypename;
        int lastDot = className.LastIndexOf('.');
        return lastDot >= 0 ? className.Substring(lastDot + 1) : className;
    }
}