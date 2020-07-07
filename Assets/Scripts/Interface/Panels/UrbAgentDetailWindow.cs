using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UrbAgentDetailWindow : UrbDisplayWindow
{
    public UrbInterfaceInput TrackAgentInput;

    public bool AgentAssigned { get; protected set; } = false;
    protected UrbAgent mAgent = null;
    public UrbAgent TargetAgent { get { return mAgent; } set { mAgent = value; AgentAssigned = value != null; } }

    public Text DisplayText;

    public override void Awake()
    {
        base.Awake();

        if(TrackAgentInput != null)
        {
            TrackAgentInput.UserAction = new UrbTrackAgent { OwningWindow = this };
        }
    }

    public void Update()
    {
        if(AgentAssigned)
        {
            if(mAgent.WasDestroyed)
            {
                DisplayText.text = "Dead";
                TargetAgent = null;
            }
            if(DisplayText != null)
            {
                DisplayText.text = TextSummary();
            }
            
        }
    }

    string TextSummary()
    {
        if (mAgent.WasDestroyed)
            return "Dead";

        string LocalName = mAgent.gameObject.name.Split('(')[0];
        string Mass = mAgent.Mass.ToString();
        string MassPerTile = mAgent.MassPerTile.ToString();
        UrbMetabolism Metabolism = mAgent.Metabolism;
        UrbBody Body = mAgent.mBody;
        UrbEater Eater = mAgent.Eater;
        UrbBreeder Breeder = mAgent.Breeder;
        UrbSmellSource Smell = mAgent.SmellSource;
        UrbThinker Thinker = mAgent.GetComponent<UrbThinker>();

        string returnText =
            "Name: " + LocalName + "\n" +
            "Mass: " + Mass + "\n" +
            "Mass Per Tile:" + MassPerTile + "\n";

        if (Metabolism != null)
        {
            returnText += "Energy: " + Metabolism.EnergyBudget + "\n";
        }

        if (Thinker != null)
        {
            string Thoughts = "Thoughts- \n";

            Thoughts += (Thinker.SafetyUrge > 0) ? "Safety Desire: " + Thinker.SafetyUrge + "\n" : "";
            Thoughts += (Thinker.BreedUrge > 0) ? "Breed Desire: " + Thinker.BreedUrge + "\n" : "";
            Thoughts += (Thinker.HungerUrge > 0) ? "Hunger Desire: " + Thinker.HungerUrge + "\n" : "";
            Thoughts += (Thinker.RestUrge > 0) ? "Rest Desire: " + Thinker.RestUrge + "\n" : "";

            returnText += Thoughts;
        }

        if (Body != null)
        {
            string BodyAnatomy = "Body- \n";
            if (Body.BodyComposition != null)
            {
                UrbSubstance[] Ingredients = Body.BodyComposition.GetCompositionIngredients();

                for (int b = 0; b < Ingredients.Length; b++)
                {
                    BodyAnatomy += Ingredients[b].Substance.ToString() + ":" + Ingredients[b].SubstanceAmount + "\n";
                }
                returnText += BodyAnatomy;
            }
        }

        if (Breeder != null)
        {
            if (Breeder.Gestating)
            {
                returnText += "Pregnant \n";
            }
        }

        if (Eater != null)
        {
            string FavoriteFood = "Diet- \n";
            for (int f = 0; f < Eater.FoodSubstances.Length; f++)
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
                returnText += FavoriteFood + StomachContents;
            }

        }

        return returnText;
    }
}

public class UrbGetAgentDetails : UrbUserAction
{
    public override string Name => "Investigate Agent";
    public override string MapDisplayAssetPath => "";
    public override void MouseClick(UrbTile currentCursorTile)
    {

        Ray mouseray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 Location = mouseray.origin;

        Collider2D Result = Physics2D.OverlapCircle(Location, 0.1f);
        if (Result!= null)
        {
            UrbAgent SelectedAgent = Result.GetComponentInParent<UrbAgent>();
            if(SelectedAgent != null)
            {
                UrbAgentDetailWindow Window = Object.Instantiate(UrbUIManager.Instance.AgentDisplayPrefab, UrbUIManager.Instance.WindowManager.transform);
                Window.TargetAgent = SelectedAgent;
            }
        }

        base.MouseClick(currentCursorTile);
    }
}

public class UrbTrackAgent : UrbUserAction
{
    public override string Name => "Track Agent";
    public override string MapDisplayAssetPath => "";

    public UrbAgentDetailWindow OwningWindow;

    public override void SelectAction()
    {
        if (OwningWindow == null)
            return;

        if (OwningWindow.InFocus || OwningWindow.MouseOver)
        {
            base.SelectAction();
            
            if(OwningWindow.AgentAssigned)
            {
                UrbCameraControls.Focus = OwningWindow.TargetAgent; 
            }
        }
    }
}