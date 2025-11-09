using System.Data;
using Unity.VisualScripting;
using UnityEngine;

public class Ufo_laser : MonoBehaviour
{

    public float fireRate = 20;
    public float laserLifttime = 0.5f;
    private float timer_fire = 0;
    private float timer_lifetime = 0;
    private bool laserIsAlive = false;

    public GameObject laserRay;
    public Transform targetTransform;
    GameObject laserObject;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (timer_fire < fireRate)
        {
            timer_fire += Time.deltaTime;
        }
        else
        {
            print("Laser fire");
            FireLaser(targetTransform.position);
            timer_fire = 0;
        }
        if (laserIsAlive) {
            if (timer_lifetime < laserLifttime)
            {
                timer_lifetime += Time.deltaTime;
            }
            else
            {
                print("Laser delete");
                timer_lifetime = 0;
                laserIsAlive = false;
                Object.Destroy(laserObject);
                // irgendwie ausfaden
            }
        }
        
    }

    private void FireLaser(Vector3 target)
    {
        
        Vector3 laserVector = transform.position - target;



        Vector3 startpos = (transform.position);
        float rotation = Vector3.SignedAngle(transform.position, target, Vector3.down);
        float length = laserVector.magnitude;
        Quaternion q_rotation = Quaternion.Euler(0, 0, 0);
        // Quaternion q_rotation = Quaternion.Euler(0, 0, rotation);
        // laserRay.transform.Rotate();
        // laserRay.transform.LookAt(target);

    

        laserObject = Instantiate(laserRay, startpos, q_rotation);
        laserObject.transform.localScale = new Vector3(0.2f, 1, length);
        laserObject.transform.LookAt(targetTransform, Vector3.forward);

        laserIsAlive = true;


    }

    [ContextMenu("Test Fire Laser")]
    private void TestFire()
    {
        Debug.Log("Test Fire");
        FireLaser(targetTransform.position);
    }

}
