using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using System.IO;

namespace Fasteroids.DataLayer
{
    public class SpaceshipRepository : IRepository
    {

        string json;
        SpaceshipInfo ship = new SpaceshipInfo();

        public SpaceshipInfo LoadData()
        {
            json = File.ReadAllText(Application.dataPath + "/spaceships.json");
            ship = JsonUtility.FromJson<SpaceshipInfo>(json);
            return ship;
        }
        public void SaveData(string name, int hScore)
        {
            ship.name = name;
            ship.highScore = hScore;
            json = JsonUtility.ToJson(ship);
            File.WriteAllText(Application.dataPath + "/spaceships.json", json);
        }
    }

    public class SpaceshipInfo
    {
        public string name;
        public int highScore;
    }
}
