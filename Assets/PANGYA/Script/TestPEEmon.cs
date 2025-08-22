using UnityEngine;



public class TestPEEmon : MonoBehaviour
{

    public GameObject cubePrefab;
    
    public float Spacing = 0.35f;
    delegate void Gooner(int GoonPOint);
    Gooner GN;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GN += GoonFirstTime;
            GN += RubilCube;

            if (GN != null)
            {
                GN(100);

                GN -= GoonFirstTime;
                GN -= RubilCube;

            }
        }
    }





    void GoonFirstTime(int GoonPOint) {
        Debug.Log("Triggered");
    }

    void RubilCube(int GoonPOint)
    {

        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                for (int k = 0; k < 5; k++)
                {
                    Instantiate(cubePrefab, new Vector3(i * Spacing, j * Spacing, k * Spacing), Quaternion.identity);
                }
            }
        
       }


        
    }





}
