using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameStep
{
    public string stepName; 
    public List<BaseInteraction> interactions = new List<BaseInteraction>(); 
}