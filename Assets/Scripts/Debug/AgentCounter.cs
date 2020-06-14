using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AgentCounter : MonoBehaviour
{
    Text mText;
    // Start is called before the first frame update
    void Start()
    {
        mText = GetComponent<Text>();
        StartCoroutine(CountCoroutine());
    }

    IEnumerator CountCoroutine()
    {
        while (true)
        {
            if (mText == null)
            {
                yield return null;
            }

            yield return new WaitForEndOfFrame();
            mText.text = "AGENTS " + UrbAgentManager.AgentCount.ToString();


        }
    }
}
