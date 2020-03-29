using System;
using UnityEngine;

[System.Serializable]
public class UrbObjectData
{
    public string Name;
    public UrbComponentData[] Components;
}

//TODO: Make this less AWFULL

public class UrbEncoder
{
    public static UrbObjectData Read(GameObject Target)
    {
        UrbObjectData ObjectData = new UrbObjectData();
        ObjectData.Name = Target.name;
        UrbBase[] cs = Target.GetComponents<UrbBase>();

        ObjectData.Components = new UrbComponentData[cs.Length];
        
        for(int c = 0; c < cs.Length; c++)
        {
            UrbComponentData ComponentData = cs[c].GetComponentData();
            ObjectData.Components[c] = ComponentData;
        }

        return ObjectData;
    }

    public static GameObject Write(UrbObjectData Data, GameObject Target)
    {
        Target.name = Data.Name;

        for(int c = 0; c < Data.Components.Length; c++)
        {
            System.Type ComponentType = System.Type.GetType(Data.Components[c].Type);
            UrbBase Component = (UrbBase)Target.GetComponent(ComponentType);

            if(Component == null)
            {
                Component = (UrbBase)Target.AddComponent(ComponentType);
            }
          
            Component.SetComponentData(Data.Components[c]);

        }

        return Target;
    }

    public static UrbFieldArrayData GetArrayFromSubstances(string ArrayName, UrbSubstance[] Substances)
    {
        UrbFieldArrayData FieldArray = new UrbFieldArrayData
        {
            Name = ArrayName,
            Value = new UrbFieldData[Substances.Length]
        };

        for (int i = 0; i < Substances.Length; i++)
        {
            FieldArray.Value[i] = new UrbFieldData { Name = Substances[i].Substance.ToString(), Value = Substances[i].SubstanceAmount };
        }
        return FieldArray;
    }

    public static UrbSubstance[] GetSubstancesFromArray(string ArrayName, UrbComponentData Data)
    {
        for (int i = 0; i < Data.FieldArrays.Length; i++)
        {
            if (Data.FieldArrays[i].Name.CompareTo(ArrayName) == 0)
            {
                return ArrayToSubstances(Data.FieldArrays[i]);
            }
        }
        return new UrbSubstance[0];
    }

    protected static UrbSubstance[] ArrayToSubstances(UrbFieldArrayData Input)
    {
        UrbSubstance[] Output = new UrbSubstance[Input.Value.Length];

        for (int a = 0; a < Input.Value.Length; a++)
        {
            Output[a] = new UrbSubstance { Substance = (UrbSubstanceTag)System.Enum.Parse(typeof(UrbSubstanceTag), Input.Value[a].Name), SubstanceAmount = Input.Value[a].Value };
        }
        return Output;
    }

    public static string GetString(string Name, UrbComponentData Data)
    {

        for (int i = 0; i < Data.Strings.Length; i++)
        {
            if (Data.Strings[i].Name.CompareTo(Name) == 0)
            {
                return Data.Strings[i].Value;
            }
        }
        return "";
    }

    public static float GetField(string Name, UrbComponentData Data)
    {
        for (int i = 0; i < Data.Fields.Length; i++)
        {
            if (Data.Fields[i].Name.CompareTo(Name) == 0)
            {
                return Data.Fields[i].Value;
            }
        }
        return 0.0f;
    }

    public static UrbRecipe GetRecipe(string Name, UrbComponentData Data)
    {
        for (int i = 0; i < Data.Recipes.Length; i++)
        {
            if (Data.Fields[i].Name.CompareTo(Name) == 0)
            {
                return Data.Recipes[i].Value;
            }
        }
        return new UrbRecipe();
    }

    public static UrbRecipe[] GetRecipeArray(string Name, UrbComponentData Data)
    {
        for (int i = 0; i < Data.RecipeArrays.Length; i++)
        {
            if (Data.RecipeArrays[i].Name.CompareTo(Name) == 0)
            {
                return Data.RecipeArrays[i].Value;
            }
        }
        return new UrbRecipe[0];
    }

    public static T GetEnum<T>(string StringName, UrbComponentData Data)
    {
        return (T)Enum.Parse(typeof(T), GetString(StringName, Data));
    }
}

[System.Serializable]
public struct UrbComponentData
{
    public string Type;
    public UrbFieldData[] Fields;
    public UrbFieldArrayData[] FieldArrays;
    public UrbRecipeData[] Recipes;
    public UrbRecipeArrayData[] RecipeArrays;
    public UrbStringData[] Strings;
}

[System.Serializable]
public struct UrbFieldData
{
    public string Name;
    public float Value;
}

[System.Serializable]
public struct UrbFieldArrayData
{
    public string Name;
    public UrbFieldData[] Value;
}

[System.Serializable]
public struct UrbRecipeData
{
    public string Name;
    public UrbRecipe Value;
}

[System.Serializable]
public struct UrbRecipeArrayData
{
    public string Name;
    public UrbRecipe[] Value;
}

[System.Serializable]
public struct UrbStringData
{
    public string Name;
    public string Value;
}
