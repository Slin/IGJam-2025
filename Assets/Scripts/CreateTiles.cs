using UnityEngine;

public class CreateTiles : MonoBehaviour
{
    public GameObject tilePrefab;
    public int tilemapRadius = 15;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        float outerRadius = 0.866025404f;
        float innerRadius = Mathf.Sqrt(3.0f) / 2.0f * outerRadius;
        float gridRadius = tilemapRadius * innerRadius;

        //Create grid of hexagonal tiles in a sphere shape around the center
        for(int x = -tilemapRadius; x <= tilemapRadius; x++)
        {
            for(int y = -tilemapRadius; y <= tilemapRadius; y++)
            {
                float positionX = x * outerRadius + y % 2.0f * outerRadius / 2.0f;
                float positionY = y * innerRadius;
                if(positionX * positionX + positionY * positionY > gridRadius * gridRadius) continue;
                Instantiate(tilePrefab, new Vector3(positionX, positionY, 0), Quaternion.identity);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
