using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
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
            yield return new WaitForEndOfFrame();
            mText.text = "AGENTS " + UrbAgentManager.AgentCount;
        }
    }
}
