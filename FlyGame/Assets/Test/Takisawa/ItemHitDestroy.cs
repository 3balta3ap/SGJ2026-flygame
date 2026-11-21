using UnityEngine;

public class ItemHitDestroy: MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
       
        Destroy(gameObject);
    }
}
