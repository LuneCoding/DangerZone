﻿using System;
using System.Collections;
using Assets.Scripts.BehaviourTree;
using Assets.Scripts.Settings;
using Assets.Scripts.Utility;
using Boo.Lang;
using UnityEngine;
using UnityEngine.AI;

namespace Assets.Scripts.Scenario
{
    /// <summary>
    /// Generates a room randomly
    /// </summary>
    public class Room : MonoBehaviour
    {
        public ObjectPool singleWall;
        public ObjectPool cornerWall;
        public ObjectPool noWall;
        public GameObject escapepoint;

        public GameObject Spawn;

        [SerializeField] private GameObject Parent;

        public List<NavMeshSurface> meshSurfaces;
        [SerializeField] public NavMeshSurface mesh;

        public bool IsLoading;

        /// <summary>
        /// Width of the bunker
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// length of the bunker
        /// </summary>
        public int Length { get; set; }

        private LoadRandom load;

        public static Room instance;

        /// <summary>
        /// array with possible furniture to spawn
        /// </summary>
        public GameObject[] FurnitureArray;

        /// <summary>
        /// all spawned furniture
        /// </summary>
        private List<GameObject> furnitureList;

        void Awake()
        {
            instance = this;
            load = new LoadRandom { difficulty = Difficulty.Door };
            IsLoading = false;
        }

        void Start()
        {
            SetLights();
        }

        /// <summary>
        /// sets size of the room
        /// </summary>
        public void SetRoomSize()
        {
            int min = ScenarioSettings.MinRoomSize;
            int max = ScenarioSettings.MaxRoomSize;
            Width = RNG.Next(min, max);
            Length = RNG.Next(min, max);
        }

        /// <summary>
        /// set the position and rotation of the spawn
        /// </summary>
        /// <returns></returns>
        public Vector3 SetSpawnPos()
        {
            //first generate number 0-3 to choose side
            int side = RNG.Next(0, 4);

            int sideLength = side % 2 == 0 ? Width : Length;

            //dont count lower and upper limit, because corners aren valid spawn positions
            int position = RNG.Next(1, sideLength - 2);

            Vector3 spawnPos;
            int spawnRot;
            switch (side)
            {
                case 0:
                    spawnPos = new Vector3(position, 0, 0);
                    spawnRot = 0;
                    break;
                case 1:
                    spawnPos = new Vector3(0, 0, position);
                    spawnRot = 90;
                    break;
                case 2:
                    spawnPos = new Vector3(position, 0, Length - 1);
                    spawnRot = 180;
                    break;
                case 3:
                    spawnPos = new Vector3(Width - 1, 0, position);
                    spawnRot = -90;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            //Spawn.transform.position = new Vector3(Spawni * 2, 0, Spawnj * 2);
            Spawn.transform.rotation = Quaternion.Euler(new Vector3(0, spawnRot, 0));
            Spawn.transform.position = spawnPos;

            return spawnPos;
        }

        /// <summary>
        /// sets a random orientation of the door in spawn gameobject
        /// </summary>
        public void SetDoor()
        {
            Transform frame = Door.instance.transform.parent;

            int side = RNG.Next(0, 2);
            //    Debug.Log(side);
            switch (side)
            {
                case 0: //rightside hinge
                    frame.localPosition = new Vector3(0, 0, .6f);
                    frame.localRotation = Quaternion.identity;
                    frame.localScale = new Vector3(1, 1, 1);
                    break;
                case 1: //leftside hinge
                    frame.localPosition = new Vector3(0, 0, .6f);
                    //frame.localRotation = Quaternion.Euler(new Vector3(0, 180, 0));
                    frame.localScale = new Vector3(-1f, 1, 1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Saves what edges the position touches
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public List<SideType> SetSidesList(int x, int y)
        {
            List<SideType> sideTypes = new List<SideType>();

            if (x == 0)
                sideTypes.Add(SideType.Up);
            else if (x == Width - 1)
                sideTypes.Add(SideType.Down);

            if (y == 0)
                sideTypes.Add(SideType.Left);
            else if (y == Length - 1)
                sideTypes.Add(SideType.Right);

            return sideTypes;
        }

        /// <summary>
        /// set object to no wall
        /// </summary>
        /// <returns></returns>
        private ObjectSpawn NoWall()
        {
            return new ObjectSpawn { objectType = ObjectType.Empty };
        }

        /// <summary>
        /// set object to single wall and set correct rotation
        /// </summary>
        /// <param name="sides"></param>
        /// <returns></returns>
        private ObjectSpawn SetSingleWall(List<SideType> sides)
        {
            ObjectSpawn objectSpawn = new ObjectSpawn { objectType = ObjectType.SingleWall };
            switch (sides[0])
            {
                case SideType.Up:
                    objectSpawn.rotation = -90;
                    break;
                case SideType.Right:
                    objectSpawn.rotation = 0;
                    break;
                case SideType.Down:
                    objectSpawn.rotation = 90;
                    break;
                case SideType.Left:
                    objectSpawn.rotation = 180;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return objectSpawn;
        }

        /// <summary>
        /// set object to corner and set correct rotation
        /// </summary>
        /// <param name="sides"></param>
        /// <returns></returns>
        private ObjectSpawn SetCornerWall(List<SideType> sides)
        {
            ObjectSpawn objectSpawn = new ObjectSpawn { objectType = ObjectType.Corner };

            if (sides.Contains(SideType.Up) && sides.Contains(SideType.Left))
                objectSpawn.rotation = 180;
            else if (sides.Contains(SideType.Up) && sides.Contains(SideType.Right))
                objectSpawn.rotation = -90;
            else if (sides.Contains(SideType.Down) && sides.Contains(SideType.Left))
                objectSpawn.rotation = 90;
            else if (sides.Contains(SideType.Down) && sides.Contains(SideType.Right))
                objectSpawn.rotation = 0;

            return objectSpawn;
        }

        /// <summary>
        /// Generate room at selected size
        /// </summary>
        public void Generate()
        {
            StartCoroutine("generateCoRoutine");
        }

        /// <summary>
        /// Generate room at selected size
        /// </summary>
        private IEnumerator generateCoRoutine()
        {
            SetRoomSize();
            Transform[] transforms = GenerateRoom();
            SetDoor();
            SetLights();
            GenerateNavMesh();
            bool[] placed = GenerateFurniture(transforms);
            GenerateEscapePoints(placed, transforms);
            yield return null;
        }

        /// <summary>
        /// Spawns escape points in the room
        /// </summary>
        /// <param name="placed"></param>
        private void GenerateEscapePoints(bool[] placed, Transform[] transforms)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Length; y++)
                {
                    //if ((x == 0 || x == Width - 1) || (y == 0 || y == Length - 1))
                    {
                        int pos = x + y * Width;
                        if (placed[pos]) continue;
                        if (transforms[pos] == null) continue;
                        if (transforms[pos].position == Door.instance.transform.position) continue;

                        GameObject go = Instantiate(escapepoint);

                        furnitureList.Add(go);
                        go.transform.position = transforms[pos].position;
                    }
                }
            }

        }

        /// <summary>
        /// spawn random amount of furniture in the room
        /// </summary>
        private bool[] GenerateFurniture(Transform[] locations)
        {
            if (furnitureList != null)
            {
                foreach (GameObject o in furnitureList)
                {
                    Destroy(o);
                }
            }
            furnitureList = new List<GameObject>();

            bool[] placed = new bool[locations.Length];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Length; y++)
                {
                    int arrayPosition = x + y * Width;
                    Transform current = locations[arrayPosition];

                    if (current == null) continue;
                    if (placed[arrayPosition]) continue;

                    switch (RNG.Next(0, 5))
                    {
                        case 0:
                        case 1:
                        case 2:
                            if (x == 0 || x == Width - 1 || y == 0 || y == Length - 1)
                            {
                                switch (RNG.Next(0, 5))
                                {
                                    case 0:
                                    case 1:
                                    case 2:
                                        SpawnFurniture(FurnitureArray[0], current, 0);
                                        placed[arrayPosition] = true;
                                        break;
                                    case 3:
                                        SpawnFurniture(FurnitureArray[3], current, 0);
                                        placed[arrayPosition] = true;
                                        break;
                                    case 4:
                                        SpawnFurniture(FurnitureArray[4], current, 0);
                                        placed[arrayPosition] = true;
                                        break;
                                }

                            }
                            break;
                        case 3:
                            if (x > 0 && x < Width - 1 && y > 0 && y < Length - 1 &&
                                !placed[(x + 1) + (y) * Width] && /*!placed[(x + 1) + (y + 1) * Width] && */
                                !placed[(x) + (y + 1) * Width] && /*!placed[(x - 1) + (y + 1) * Width] &&*/
                                !placed[(x - 1) + (y) * Width] && /*!placed[(x - 1) + (y - 1) * Width] &&*/
                                !placed[(x) + (y - 1) * Width]/* && !placed[(x - 1) + (y - 1) * Width]*/)
                            {
                                SpawnFurniture(FurnitureArray[1], current, 0);
                                placed[arrayPosition] = true;

                                SpawnChair((x + 1), y, placed, locations, 90);
                                SpawnChair((x - 1), y, placed, locations, 270);
                                SpawnChair((x), (y + 1), placed, locations, 0);
                                SpawnChair((x), (y - 1), placed, locations, 180);
                            }
                            else
                            {
                                //   SpawnFurniture(FurnitureArray[4], current, 0);
                                // placed[arrayPosition] = true;
                            }
                            break;
                    }
                }
            }
            return placed;
        }

        public void SpawnFurniture(GameObject furniture, Transform current, int rotation)
        {
            furniture.transform.position = current.position;
            furniture.transform.rotation = current.rotation * Quaternion.Euler(0, rotation, 0);
            furnitureList.Add(Instantiate(furniture));
        }

        public void SpawnChair(int x, int y, bool[] placed, Transform[] locations, int rotation)
        {
            int pos = x + y * Width;
            if (pos < Width * Length)
            {
                if (8 % RNG.Next(1, 8) != 0) return;
                if (placed[pos]) return;
                if (locations[pos] == null) return;
                if (locations[pos].rotation.eulerAngles.y > 0) rotation = 0;

                SpawnFurniture(FurnitureArray[2], locations[pos], rotation);
                placed[pos] = true;
            }
        }

        /// <summary>
        /// check if furniture is inside another furniture. because unity doesnt do that
        /// </summary>
        /// <param name="centerposition"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private bool CheckNavmeshObstacle(Vector3 centerposition, Vector3 size)
        {
            foreach (GameObject furniture in furnitureList)
            {
                Bounds bounds = furniture.GetComponentInChildren<BoxCollider>().bounds;
                //middle
                if (bounds.Contains(centerposition))
                    return false;
                //upperleft
                if (bounds.Contains(centerposition + new Vector3(-size.x, 0, -size.z) / 2))
                    return false;
                //upperright
                if (bounds.Contains(centerposition + new Vector3(size.x, 0, -size.z) / 2))
                    return false;
                //bottomleft
                if (bounds.Contains(centerposition + new Vector3(-size.x, 0, size.z) / 2))
                    return false;
                //bottomright
                if (bounds.Contains(centerposition + new Vector3(size.x, 0, size.z) / 2))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Generate Room at size selected
        /// </summary>
        private Transform[] GenerateRoom()
        {
            Transform[] transforms = new Transform[Width * Length];

            IsLoading = true;
            meshSurfaces = new List<NavMeshSurface>();
            Vector3 spawnPos = SetSpawnPos();

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Length; y++)
                {
                    List<SideType> sides = SetSidesList(x, y);
                    //check what object type is needed
                    ObjectSpawn objectSpawn;
                    switch (sides.Count)
                    {
                        case 0:
                            objectSpawn = NoWall();
                            break;
                        case 1:
                            objectSpawn = SetSingleWall(sides);
                            break;
                        case 2:
                            objectSpawn = SetCornerWall(sides);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    GameObject tempObj;

                    //overwrite walltype to empty if spawn attaches to scene here
                    if (x == spawnPos.x && y == spawnPos.z)
                        objectSpawn.objectType = ObjectType.Empty;

                    //Get selected object from objectpool
                    switch (objectSpawn.objectType)
                    {
                        case ObjectType.Empty:
                            tempObj = noWall.GetObject();
                            break;
                        case ObjectType.SingleWall:
                            tempObj = singleWall.GetObject();
                            break;
                        case ObjectType.Corner:
                            tempObj = cornerWall.GetObject();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    //apply correct position and rotation to object
                    tempObj.transform.position = new Vector3(x, 0, y);
                    tempObj.transform.rotation = Quaternion.Euler(new Vector3(0, objectSpawn.rotation, 0));
                    tempObj.SetActive(true);

                    if (!(x == spawnPos.x && y == spawnPos.z))
                    {
                        transforms[x + y * Width] = (tempObj.transform);
                    }
                }
            }
            IsLoading = false;

            return transforms;
        }

        /// <summary>
        /// delete Room
        /// </summary>
        public void DeleteRoom()
        {
            noWall.Reset();
            singleWall.Reset();
            cornerWall.Reset();
        }

        /// <summary>
        /// generate navmesh for npcs
        /// </summary>
        /// <remarks>For some strange reason you only need to have 1 navmeshsurface.</remarks>
        public void GenerateNavMesh()
        {
            mesh.BuildNavMesh();
        }

        /// <summary>
        /// set lights on or off
        /// </summary>
        public void SetLights()
        {
            foreach (Light component in cornerWall.GetComponentsInChildren<Light>())
                component.enabled = ScenarioSettings.Lights;

            foreach (Light componentsInChild in Spawn.transform.Find("Models").GetComponentsInChildren<Light>())
                componentsInChild.enabled = ScenarioSettings.Lights;
        }
    }
}