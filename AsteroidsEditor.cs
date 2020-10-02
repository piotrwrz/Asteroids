using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using System;

public class AsteroidsEditor : EditorWindow
{
    public Vector2 pos;
    public float angle, radians;
    public bool toggle;
    public Vector4 rotationComp, degreesRotation;
    public Quaternion q = new Quaternion(1, 0, 0, 0);
    public GameObject asteroidPrefab;
    public GameObject gameLogicObj;
    public GameLogic gameLogic;
    public GameObject asteroids;

    public int i = 0;
    [MenuItem("Window/Asteroids Editor")]

    public static void ShowWindow()
    {
        GetWindow<AsteroidsEditor>("Asteroids Editor");
    }


    void OnGUI()
    {
        gameLogicObj = EditorGUILayout.ObjectField("Game Logic", gameLogicObj, typeof(GameObject), true) as GameObject;
        gameLogicObj = GameObject.FindGameObjectWithTag("Player");
        gameLogic = gameLogicObj.GetComponent<GameLogic>();
        i = 0;
        for (int j = 0; j < 40; j++)
        {
            if (gameLogic._asteroidPool[j] != null)
            {
                i++;
            }
        }
        asteroidPrefab = EditorGUILayout.ObjectField("Asteroid Prefab", asteroidPrefab, typeof(GameObject), true) as GameObject;
        pos = EditorGUI.Vector2Field(new Rect(3, 60, position.width - 6, 20), "Position:", pos);
        angle = EditorGUI.FloatField(new Rect(3, 100, position.width - 6, 20), "Degrees:", angle);
        radians = EditorGUI.FloatField(new Rect(3, 122, position.width - 6, 20), "Radians:", radians);
        toggle = EditorGUI.Toggle(new Rect(3, 145, position.width - 6, 20), "Use radians", toggle);
        i = EditorGUI.IntField(new Rect(3, 170, position.width - 6, 20), "New asteroids:", i);


        ///Choose between degrees and radians
        if (!toggle)
            radians = angle * (Mathf.PI / 180);
        angle = 180 * radians / Mathf.PI;



        if (GUILayout.Button("Spawn asteroid"))
        {
            SpawnAsteroid(pos, radians);
        }
    }

    public void SpawnAsteroid(Vector2 pos, float radians)
    { 
        ///Creating asteroid in EditorMode
        asteroids = Instantiate(asteroidPrefab);
        asteroids.transform.position = new Vector3(pos.x, pos.y, 0.4f);

        ///Sending asteroid to gameLogic to make it part of a simulation
        gameLogic._asteroidPool[i] = asteroids;
        gameLogic.newAsteroidsV2[i] = new Vector2(pos.x,pos.y);
        gameLogic.newAsteroidsRadian[i] = radians;
    }

}
