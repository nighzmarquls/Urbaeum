using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPSCounter : MonoBehaviour
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
            yield return new WaitForSeconds(0.05f);
            float FPS = Mathf.Round( (1 / Time.deltaTime));
 
            if (FPS > 60)
            {
                mText.color = Color.cyan;
            }
            else if (FPS > 30)
            {
                mText.color = Color.green;
            }
            else if (FPS > 15)
            {
                mText.color = Color.yellow;
            }
            else
            {
                mText.color = Color.red;
            }

            mText.text = FPS.ToString();


        }
    }
}
