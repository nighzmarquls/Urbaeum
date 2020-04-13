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

    public static T GetEnum<T>(string Name, UrbComponentData Data)
    {
        return (T)Enum.Parse(typeof(T), GetString(Name, Data));
    }

    public static UrbStringArrayData EnumsToArray<T>(string Name, T[] Enums) where T : Enum
    {
        UrbStringArrayData Data = new UrbStringArrayData
        {
            Name = Name,
            Value = new string[Enums.Length]
        };
        for(int e = 0; e < Enums.Length; e++)
        {
            Data.Value[e] = Enums[e].ToString();
        }

        return Data;
    }

    public static T[] GetEnumArray<T>(string Name, UrbComponentData Data)
    {
        for (int i = 0; i < Data.StringArrays.Length; i++)
        {
            if (Data.StringArrays[i].Name.CompareTo(Name) == 0)
            {

                T[] Output = new T[Data.StringArrays[i].Value.Length];

                for(int a = 0; a < Data.StringArrays[i].Value.Length; a++)
                {
                    Output[a] = (T)Enum.Parse(typeof(T), Data.StringArrays[i].Value[a]);
                }

                return Output;
            }
        }

        return new T[0];
    }

    public static UrbStringArrayData ObjectsDataToArray(string Name, UrbObjectData[] Objects)
    {
        if(Objects == null)
        {
            return new UrbStringArrayData {
                Name = Name,
                Value = new string[0]};
        }
        UrbStringArrayData Data = new UrbStringArrayData
        {
            Name = Name,
            Value = new string[Objects.Length]
        };
        for (int o = 0; o < Objects.Length; o++)
        {
            Data.Value[o] = JsonUtility.ToJson(Objects[o], true);
        }

        return Data;
    }

    public static UrbObjectData GetObjectData(string Name, UrbComponentData Data)
    {
        return JsonUtility.FromJson<UrbObjectData>(GetString(Name, Data));
    }

    public static UrbObjectData[] GetObjectDataArray(string Name, UrbComponentData Data)
    {
        for (int i = 0; i < Data.StringArrays.Length; i++)
        {
            if (Data.StringArrays[i].Name.CompareTo(Name) == 0)
            {

                UrbObjectData[] Output = new UrbObjectData[Data.StringArrays[i].Value.Length];

                for (int o = 0; o < Data.StringArrays[i].Value.Length; o++)
                {
                    Output[o] = JsonUtility.FromJson<UrbObjectData>(Data.StringArrays[i].Value[o]);
                }

                return Output;
            }
        }

        return new UrbObjectData[0];
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
    public UrbStringArrayData[] StringArrays;
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

[System.Serializable]
public struct UrbStringArrayData
{
    public string Name;
    public string[] Value;
}