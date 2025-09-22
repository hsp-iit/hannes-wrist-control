using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Diagnostics = System.Diagnostics;

public class MoveProsthesisAutomatically
{
    private class Pose
    {
        public Vector3 position;
        public Quaternion rotation;

        public Pose(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }
    }

    private List<Pose> poses;
    [HideInInspector]
    public int countPoses;
    private int iterationsPerPose;
    private int countIterations;

    private GameObject armBase;
    
    public Quaternion initialWristRotation = Quaternion.Euler(29.2f, -1f, 1.686f);

    private bool stopExecution;
    

    private void checkGameObjectConsistency(GameObject armBase)
    {
        if (armBase.name != "Arm Movements Base")
        {
            Debug.LogError("Wrong GameObject received for armBase parameter. " +
                           "The poses in this script are meant for the Arm Movements Base GameObject," +
                           "but another GameObject has been received here. Stopping Execution...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }
        if (armBase.transform.Find("Hannes_ARM") == null)
        {
            Debug.LogError("The Arm Movements Base GameObject must have the Hannes_ARM " +
                           "GameObject as a child. Stopping execution...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }
    }


    public MoveProsthesisAutomatically(int iterationsPerPose, GameObject armBase)
    {
        checkGameObjectConsistency(armBase);

        this.iterationsPerPose = iterationsPerPose;
        this.countIterations = this.iterationsPerPose + 1;
        
        this.countPoses = -1;
        
        this.armBase = armBase;

        this.stopExecution = false;

        poses = new List<Pose>();

        Vector3 position;
        Quaternion rotation;
        Pose pose;

        // idx pose: 0
        position = new Vector3(-0.5f, 0.31f, -0.61f);
        rotation = Quaternion.Euler(-39.141f, 14.838f, -23.71f);
        pose = new Pose(position, rotation);
        poses.Add(pose);

        // idx pose: 1
        position = new Vector3(-0.76f, 0.3f, -0.05f);
        rotation = Quaternion.Euler(-30.769f, 30.008f, -38.147f);
        pose = new Pose(position, rotation);
        poses.Add(pose);

        // idx pose: 2
        position = new Vector3(-0.44f, 0.64f, -0.45f);
        rotation = Quaternion.Euler(-19.472f, 7.159f, 3.29f);
        pose = new Pose(position, rotation);
        poses.Add(pose);

        // idx pose: 3
        position = new Vector3(-0.44f, 0.64f, -0.45f);
        rotation = Quaternion.Euler(-19.472f, 7.159f, 3.29f);
        pose = new Pose(position, rotation);
        poses.Add(pose);

        // idx pose: 4
        position = new Vector3(-0.55f, 0.57f, -0.04f);
        rotation = Quaternion.Euler(-32.247f, 4.348f, 11.84f);
        pose = new Pose(position, rotation);
        poses.Add(pose);

        // idx pose: 5
        position = new Vector3(0.13f, 0.9f, -0.49f);
        rotation = Quaternion.Euler(-18.784f, 6.992f, -0.629f);
        pose = new Pose(position, rotation);
        poses.Add(pose);

        // idx pose: 6
        position = new Vector3(-0.85f, 0.94f, -0.49f);
        rotation = Quaternion.Euler(-18.784f, 6.992f, -0.629f);
        pose = new Pose(position, rotation);
        poses.Add(pose);

        // idx pose: 7
        position = new Vector3(-0.90f, 0.04f, -0.49f);
        rotation = Quaternion.Euler(-37.18f, 6.992f, -0.629f);
        pose = new Pose(position, rotation);
        poses.Add(pose);

        // idx pose: 8
        position = new Vector3(-0.90f, 1.57f, -0.24f);
        rotation = Quaternion.Euler(-15.61f, 6.992f, 3.91f);
        pose = new Pose(position, rotation);
        poses.Add(pose);

        // idx pose: 9
        position = new Vector3(-1f, 1.2f, -0.24f);
        rotation = Quaternion.Euler(-15.61f, 24.87f, 6.12f);
        pose = new Pose(position, rotation);
        poses.Add(pose);

        // idx pose: 10
        position = new Vector3(-0.90f, 1.57f, -0.24f);
        rotation = Quaternion.Euler(-15.61f, 28.1f, -15.08f);
        pose = new Pose(position, rotation);
        poses.Add(pose);

        // idx pose: 11
        position = new Vector3(-0.33f, 1.57f, -0.24f);
        rotation = Quaternion.Euler(-1.4f, 28.1f, -15.08f);
        pose = new Pose(position, rotation);
        poses.Add(pose);

        // idx pose: 12
        position = new Vector3(-0.90f, 0.67f, -0.49f);
        rotation = Quaternion.Euler(-31.47f, 6.992f, 3.91f);
        pose = new Pose(position, rotation);
        poses.Add(pose);

        // idx pose: 13
        position = new Vector3(-0.90f, 0.80f, -0.49f);
        rotation = Quaternion.Euler(-18.784f, 6.992f, 23.96f);
        pose = new Pose(position, rotation);
        poses.Add(pose);

        // idx pose: 14
        position = new Vector3(0.2f, 0.67f, -0.49f);
        rotation = Quaternion.Euler(-31.47f, 6.992f, -31.65f);
        pose = new Pose(position, rotation);
        poses.Add(pose);

        // idx pose: 15
        position = new Vector3(0.36f, 0.67f, -0.49f);
        rotation = Quaternion.Euler(-31.47f, -2.35f, 3.91f);
        pose = new Pose(position, rotation);
        poses.Add(pose);

        // idx pose: 16
        position = new Vector3(0.01f, 0.6f, -0.49f);
        rotation = Quaternion.Euler(-34.69f, -2.35f, 15.23f);
        pose = new Pose(position, rotation);
        poses.Add(pose);

        // idx pose: 17
        position = new Vector3(0.36f, 0.12f, -0.9f);
        rotation = Quaternion.Euler(-35.72f, -2.35f, 3.91f);
        pose = new Pose(position, rotation);
        poses.Add(pose);

        // idx pose: 18
        position = new Vector3(0.36f, 0.12f, -0.9f);
        rotation = Quaternion.Euler(-46.8f, -2.35f, 3.91f);
        pose = new Pose(position, rotation);
        poses.Add(pose);

        // idx pose: 19
        position = new Vector3(0.1f, -0.6f, -0.98f);
        rotation = Quaternion.Euler(-54.51f, -14.8f, 30.87f);
        pose = new Pose(position, rotation);
        poses.Add(pose);
    }

    public bool checkIterationsAndUpdateArmPose()
    {
        if (iterationsPerPose <= 0)
            return false;

        countIterations += 1;

        if (this.stopExecution && countIterations > iterationsPerPose / 4)
        {
            Debug.LogError("...now stop exeuction");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
            return false;
        }

        if (countIterations < iterationsPerPose)
            return false;

        countPoses += 1;
        if (countPoses == poses.Count)
        {
            countPoses = 0;
            this.stopExecution = true;
            Debug.LogError("All poses executed (" + poses.Count + "/" + poses.Count + "), " + 
                           "wait a while before stopping execution...");
        }
        else
        { 
            Debug.Log("Executing pose " + countPoses);
        }
            
        countIterations = 0;

        armBase.transform.localPosition = poses[countPoses].position;
        armBase.transform.localRotation = poses[countPoses].rotation;

        return true;
    }

}
