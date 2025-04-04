using System.Collections.Generic;
using UnityEngine;

public class Floor : MonoBehaviour
{
    [SerializeReference] public List<FloorEffect> objectives = new List<FloorEffect>();
}
