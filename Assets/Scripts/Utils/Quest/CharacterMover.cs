using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterMover : MonoBehaviour
{
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// 沿路径移动，返回协程
    /// </summary>
    public IEnumerator MoveAlongPath(List<Vector2> waypoints, float speed)
    {
        if (waypoints == null || waypoints.Count == 0) yield break;

        int index = 0;
        Vector2 target = waypoints[0];
        while (index < waypoints.Count)
        {
            target = waypoints[index];
            Vector2 newPos = Vector2.MoveTowards(rb.position, target, speed * Time.deltaTime);
            rb.MovePosition(newPos);
            if (Vector2.Distance(rb.position, target) < 0.05f)
                index++;
            yield return null;
        }
    }
}