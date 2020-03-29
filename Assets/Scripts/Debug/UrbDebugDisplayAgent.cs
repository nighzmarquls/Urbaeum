using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UrbDebugDisplayAgent : MonoBehaviour
{
    Text mText;
    public UrbMap targetMap;

    UrbAgent currentAgent = null;
    // Start is called before the first frame update
    void Start()
    {
        mText = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        if (mText == null || targetMap == null)
            return;

        Ray mouseray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 Location = mouseray.origin + (mouseray.direction * (Vector3.Distance(mouseray.origin, transform.position)));

        UrbTile Tile = targetMap.GetNearestTile(Location);

        if (Tile != null)
        {
            UrbAgent NewAgent = Tile.CurrentContent;

            if (NewAgent != null)
            {
                
                    DisplayAgent(NewAgent);
                
            }
            else if(currentAgent != null)
            {
                DisplayAgent(currentAgent);
            }
            else
            {
                mText.text = "";
            }
        }
        
    }

    void DisplayAgent(UrbAgent Input)
    {
        currentAgent = Input;

        string LocalName = Input.gameObject.name.Split('(')[0];
        string Age = Mathf.Round(Time.time - Input.BirthTime).ToString();
        UrbBody Body = Input.Body;
        UrbEater Eater = Input.GetComponent<UrbEater>();
        UrbBreeder Breeder = Input.GetComponent<UrbBreeder>();
        UrbSmellSource Smell = Input.GetComponent<UrbSmellSource>();
        UrbThinker Thinker = Input.GetComponent<UrbThinker>();

        string displayText =
            "Name: " + LocalName + "\n" +
            "Age: " + Age + "\n";

        if(Thinker != null)
        {
            string Thoughts = "Thoughts- \n";

            Thoughts += (Thinker.BreedUrge > 0) ? "Breed Desire: " + Thinker.BreedUrge + "\n" : "";
            Thoughts += (Thinker.HungerUrge > 0) ? "Hunger Desire: " + Thinker.HungerUrge + "\n" : "";
            Thoughts += (Thinker.RestUrge > 0) ? "Rest Desire: " + Thinker.RestUrge + "\n" : "";

            displayText += Thoughts;
        }

        if(Body != null)
        {
            string BodyAnatomy ="Body- \n";
            if (Body.BodyComposition != null)
            {
                UrbSubstance[] Ingredients = Body.BodyComposition.GetCompositionIngredients();

                for (int b = 0; b < Ingredients.Length; b++)
                {
                    BodyAnatomy += Ingredients[b].Substance.ToString() + ":" + Ingredients[b].SubstanceAmount + "\n";
                }
                displayText += BodyAnatomy;
            }
        }

        if (Breeder != null)
        {
            if (Breeder.Gestating)
            {
                displayText += "Pregnant \n";
            }
        }

        if (Eater != null)
        {
            string FavoriteFood = "Diet- \n";
            for( int f = 0; f < Eater.FoodSubstances.Length; f++)
            {
                FavoriteFood += Eater.FoodSubstances[f].ToString() + "\n";
            }
            if (Eater.Stomach != null)
            {
                UrbSubstance[] Ingredients = Eater.Stomach.GetCompositionIngredients();

                string StomachContents = "Stomach Contents- \n";
                for (int b = 0; b < Ingredients.Length; b++)
                {
                    if (Ingredients[b].SubstanceAmount > 0)
                    {
                        StomachContents += Ingredients[b].Substance.ToString() + ":" + Ingredients[b].SubstanceAmount + "\n";
                    }
                }
                displayText += FavoriteFood + StomachContents;
            }

        }

        mText.text = displayText;

    }
}
