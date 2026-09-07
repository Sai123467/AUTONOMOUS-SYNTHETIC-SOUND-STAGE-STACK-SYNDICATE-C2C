using UnityEngine;
using System.Collections.Generic;

public class ProceduralSceneryBuilder : MonoBehaviour
{
    [Header("Engine References")]
    public ProceduralSoundstage soundstage;
    public Light directionalSun;

    private GameObject winterRoot;
    private GameObject summerRoot;
    private GameObject springRoot;
    private GameObject autumnRoot;

    private ParticleSystem snowEffect;
    private ParticleSystem rainEffect;
    private ParticleSystem leavesEffect;
    private ParticleSystem springRiverSplashes;

    private Season lastSeason = (Season)(-1);

    void Start()
    {
        BuildCompleteScenery();
        if (soundstage != null)
        {
            ApplySeasonVisuals(soundstage.currentSeason);
        }
    }

    void Update()
    {
        if (soundstage != null && soundstage.currentSeason != lastSeason)
        {
            ApplySeasonVisuals(soundstage.currentSeason);
        }
    }

    [ContextMenu("Rebuild Scenery")]
    public void BuildCompleteScenery()
    {
        Transform oldRoot = transform.Find("Procedural_World");
        if (oldRoot != null)
        {
            if (Application.isPlaying) Destroy(oldRoot.gameObject);
            else DestroyImmediate(oldRoot.gameObject);
        }

        GameObject worldRoot = new GameObject("Procedural_World");
        worldRoot.transform.SetParent(transform);

        if (directionalSun == null)
        {
            Light foundLight = FindAnyObjectByType<Light>();
            if (foundLight != null && foundLight.type == LightType.Directional)
                directionalSun = foundLight;
            else
            {
                GameObject sunObj = new GameObject("Procedural_Sun");
                directionalSun = sunObj.AddComponent<Light>();
                directionalSun.type = LightType.Directional;
            }
        }

        winterRoot = new GameObject("Season_Winter");
        winterRoot.transform.SetParent(worldRoot.transform);

        summerRoot = new GameObject("Season_Summer");
        summerRoot.transform.SetParent(worldRoot.transform);

        springRoot = new GameObject("Season_Spring");
        springRoot.transform.SetParent(worldRoot.transform);

        autumnRoot = new GameObject("Season_Autumn");
        autumnRoot.transform.SetParent(worldRoot.transform);

        PopulateSeasonBiome(winterRoot, new Color(0.92f, 0.95f, 1.0f), new Color(0.85f, 0.92f, 0.98f), Season.Winter);
        PopulateSeasonBiome(summerRoot, new Color(0.35f, 0.58f, 0.22f), new Color(0.18f, 0.45f, 0.12f), Season.Summer);
        PopulateSeasonBiome(springRoot, new Color(0.25f, 0.65f, 0.30f), new Color(0.95f, 0.55f, 0.70f), Season.Spring);
        PopulateSeasonBiome(autumnRoot, new Color(0.60f, 0.38f, 0.18f), new Color(0.85f, 0.35f, 0.08f), Season.Autumn);

        snowEffect = CreateWeatherParticles("Snow_Emitter", worldRoot.transform, new Color(1f, 1f, 1f, 0.8f), 150, 4f, 0.25f, false);
        rainEffect = CreateWeatherParticles("Rain_Emitter", worldRoot.transform, new Color(0.7f, 0.85f, 1f, 0.55f), 140, 16f, 0.06f, true);
        leavesEffect = CreateWeatherParticles("Leaves_Emitter", worldRoot.transform, new Color(0.85f, 0.4f, 0.1f, 0.9f), 60, 2.5f, 0.35f, false);

        springRiverSplashes = CreateRiverSplashParticles("Spring_River_Splashes", springRoot.transform);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
    }

    private void PopulateSeasonBiome(GameObject parent, Color groundColor, Color foliageColor, Season season)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(parent.transform);
        ground.transform.localPosition = Vector3.zero;
        ground.transform.localScale = new Vector3(10f, 1f, 10f);

        Material groundMat = CreateFlatColorMaterial(groundColor, season == Season.Winter ? 0.7f : 0.1f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        System.Random rnd = new System.Random((int)season * 150);

        Material trunkMat = CreateFlatColorMaterial(new Color(0.30f, 0.18f, 0.10f), 0.0f);
        Material leavesMat = CreateFlatColorMaterial(foliageColor, 0.1f);
        Material altLeavesMat = CreateFlatColorMaterial(Color.Lerp(foliageColor, Color.black, 0.2f), 0.1f);
        Material rockMat = CreateFlatColorMaterial(new Color(0.45f, 0.45f, 0.48f), 0.1f);

        Color[] flowerColors;
        if (season == Season.Spring)
        {
            flowerColors = new Color[] {
                new Color(1.0f, 0.4f, 0.7f),
                new Color(0.95f, 0.85f, 0.2f),
                new Color(0.7f, 0.4f, 0.95f),
                new Color(1.0f, 1.0f, 1.0f)
            };
        }
        else if (season == Season.Summer)
        {
            flowerColors = new Color[] {
                new Color(0.95f, 0.2f, 0.2f),
                new Color(1.0f, 0.8f, 0.1f),
                new Color(0.2f, 0.6f, 1.0f),
                new Color(1.0f, 0.5f, 0.1f)
            };
        }
        else if (season == Season.Autumn)
        {
            flowerColors = new Color[] {
                new Color(0.8f, 0.25f, 0.1f),
                new Color(0.6f, 0.15f, 0.3f),
                new Color(0.85f, 0.55f, 0.1f)
            };
        }
        else
        {
            flowerColors = new Color[] {
                new Color(0.85f, 0.95f, 1.0f),
                new Color(0.95f, 0.98f, 1.0f)
            };
        }

        Material stemMat = CreateFlatColorMaterial(season == Season.Winter ? new Color(0.4f, 0.5f, 0.5f) : new Color(0.2f, 0.45f, 0.15f), 0.05f);

        int treeCount = 180;
        for (int i = 0; i < treeCount; i++)
        {
            float x = (float)(rnd.NextDouble() * 88.0 - 44.0);
            float z = (float)(rnd.NextDouble() * 88.0 - 44.0);

            // Avoid spawning in river, player start, and cave area (Cave center at X: 30, Z: 25)
            if (Mathf.Abs(x) < 7.0f) continue;
            if (Vector2.Distance(new Vector2(x, z), new Vector2(30f, 25f)) < 18.0f) continue;
            if (Mathf.Abs(x) < 4.5f && Mathf.Abs(z) < 4.5f) continue;

            float heightMod = (float)(rnd.NextDouble() * 0.8 + 0.8);
            float widthMod = (float)(rnd.NextDouble() * 0.4 + 0.8);
            Material chosenFoliageMat = (i % 2 == 0) ? leavesMat : altLeavesMat;

            GameObject treeGroup = new GameObject("Tree_" + i);
            treeGroup.transform.SetParent(parent.transform);
            treeGroup.transform.localPosition = new Vector3(x, 0f, z);

            if (i % 3 == 0 || season == Season.Winter)
            {
                GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.name = "Pine_Trunk";
                trunk.transform.SetParent(treeGroup.transform);
                trunk.transform.localPosition = new Vector3(0f, 2.2f * heightMod, 0f);
                trunk.transform.localScale = new Vector3(0.4f * widthMod, 2.2f * heightMod, 0.4f * widthMod);
                trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;

                for (int tier = 0; tier < 3; tier++)
                {
                    GameObject pineLayer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    pineLayer.name = "Pine_Tier_" + tier;
                    pineLayer.transform.SetParent(treeGroup.transform);
                    float yPos = (2.6f + tier * 1.4f) * heightMod;
                    float tierScale = (3.2f - tier * 0.75f) * widthMod;
                    pineLayer.transform.localPosition = new Vector3(0f, yPos, 0f);
                    pineLayer.transform.localScale = new Vector3(tierScale, 1.3f * heightMod, tierScale);
                    pineLayer.GetComponent<Renderer>().sharedMaterial = chosenFoliageMat;
                }
            }
            else
            {
                GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.name = "Broad_Trunk";
                trunk.transform.SetParent(treeGroup.transform);
                trunk.transform.localPosition = new Vector3(0f, 1.8f * heightMod, 0f);
                trunk.transform.localScale = new Vector3(0.5f * widthMod, 1.8f * heightMod, 0.5f * widthMod);
                trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;

                GameObject mainCanopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mainCanopy.name = "Main_Canopy";
                mainCanopy.transform.SetParent(treeGroup.transform);
                mainCanopy.transform.localPosition = new Vector3(0f, 4.0f * heightMod, 0f);
                mainCanopy.transform.localScale = new Vector3(3.6f * widthMod, 3.2f * heightMod, 3.6f * widthMod);
                mainCanopy.GetComponent<Renderer>().sharedMaterial = chosenFoliageMat;

                GameObject subCanopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                subCanopy.name = "Sub_Canopy";
                subCanopy.transform.SetParent(treeGroup.transform);
                subCanopy.transform.localPosition = new Vector3(0.8f * widthMod, 4.6f * heightMod, -0.6f * widthMod);
                subCanopy.transform.localScale = new Vector3(2.5f * widthMod, 2.5f * heightMod, 2.5f * widthMod);
                subCanopy.GetComponent<Renderer>().sharedMaterial = altLeavesMat;
            }

            if (i % 3 == 1)
            {
                float sx = (float)(rnd.NextDouble() * 3.0 - 1.5);
                float sz = (float)(rnd.NextDouble() * 3.0 - 1.5);
                GameObject sapling = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sapling.name = "Sapling";
                sapling.transform.SetParent(treeGroup.transform);
                sapling.transform.localPosition = new Vector3(sx, 1.4f, sz);
                sapling.transform.localScale = new Vector3(1.2f, 1.8f, 1.2f);
                sapling.GetComponent<Renderer>().sharedMaterial = chosenFoliageMat;
            }

            if (i % 4 == 0)
            {
                float rx = (float)(rnd.NextDouble() * 4.0 - 2.0);
                float rz = (float)(rnd.NextDouble() * 4.0 - 2.0);
                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = "Boulder";
                rock.transform.SetParent(treeGroup.transform);
                rock.transform.localPosition = new Vector3(rx, 0.4f, rz);
                rock.transform.localRotation = Quaternion.Euler((float)rnd.NextDouble() * 45f, (float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 45f);
                rock.transform.localScale = new Vector3(1.4f, 0.9f, 1.5f) * (float)(rnd.NextDouble() * 0.6 + 0.7);
                rock.GetComponent<Renderer>().sharedMaterial = rockMat;
            }
        }

        // Wildflowers
        int flowerCount = (season == Season.Spring || season == Season.Summer) ? 160 : 45;
        for (int j = 0; j < flowerCount; j++)
        {
            float fx = (float)(rnd.NextDouble() * 86.0 - 43.0);
            float fz = (float)(rnd.NextDouble() * 86.0 - 43.0);

            if (Mathf.Abs(fx) < 6.5f) continue;
            if (Vector2.Distance(new Vector2(fx, fz), new Vector2(30f, 25f)) < 16.0f) continue;

            Color petalColor = flowerColors[rnd.Next(flowerColors.Length)];
            Material petalMat = CreateFlatColorMaterial(petalColor, 0.2f);

            GameObject flowerGroup = new GameObject("Flower_Cluster");
            flowerGroup.transform.SetParent(parent.transform);
            flowerGroup.transform.localPosition = new Vector3(fx, 0f, fz);

            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.name = "Stem";
            stem.transform.SetParent(flowerGroup.transform);
            float stemHeight = (float)(rnd.NextDouble() * 0.25 + 0.35);
            stem.transform.localPosition = new Vector3(0f, stemHeight * 0.5f, 0f);
            stem.transform.localScale = new Vector3(0.04f, stemHeight * 0.5f, 0.04f);
            stem.GetComponent<Renderer>().sharedMaterial = stemMat;

            GameObject blossom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            blossom.name = "Blossom";
            blossom.transform.SetParent(flowerGroup.transform);
            blossom.transform.localPosition = new Vector3(0f, stemHeight, 0f);
            float blossomSize = (float)(rnd.NextDouble() * 0.15 + 0.25);
            blossom.transform.localScale = new Vector3(blossomSize * 1.3f, blossomSize * 0.7f, blossomSize * 1.3f);
            blossom.GetComponent<Renderer>().sharedMaterial = petalMat;

            if (season == Season.Autumn && j % 2 == 0)
            {
                blossom.name = "Mushroom_Cap";
                blossom.transform.localScale = new Vector3(0.45f, 0.15f, 0.45f);
                stem.transform.localScale = new Vector3(0.08f, 0.2f, 0.08f);
                stem.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                blossom.transform.localPosition = new Vector3(0f, 0.38f, 0f);
            }
        }

        // Low-Poly Bushes
        int bushCount = (season == Season.Winter) ? 20 : 45;
        for (int b = 0; b < bushCount; b++)
        {
            float bx = (float)(rnd.NextDouble() * 84.0 - 42.0);
            float bz = (float)(rnd.NextDouble() * 84.0 - 42.0);
            if (Mathf.Abs(bx) < 6.5f) continue;
            if (Vector2.Distance(new Vector2(bx, bz), new Vector2(30f, 25f)) < 16.0f) continue;

            GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.name = "Bush";
            bush.transform.SetParent(parent.transform);
            float bScale = (float)(rnd.NextDouble() * 0.5 + 0.6);
            bush.transform.localPosition = new Vector3(bx, bScale * 0.45f, bz);
            bush.transform.localScale = new Vector3(bScale * 1.4f, bScale * 0.8f, bScale * 1.2f);
            bush.GetComponent<Renderer>().sharedMaterial = leavesMat;
        }

        PopulateUltraDenseGrass(parent, season, rnd);
        BuildSeasonalRiver(parent, season, rnd);
        BuildScaryExpansiveCavern(parent, season);
        SpawnSeasonalWildlife(parent, season);
    }

    // Builds a large, fully walk-in, terrifying cavern at X = 30, Z = 25
    private void BuildScaryExpansiveCavern(GameObject parent, Season season)
    {
        GameObject caveRoot = new GameObject("Cavern_Complex");
        caveRoot.transform.SetParent(parent.transform);
        caveRoot.transform.localPosition = new Vector3(30f, 0f, 25f);

        Material darkRockMat = CreateFlatColorMaterial(new Color(0.10f, 0.09f, 0.12f), 0.05f);
        Material bloodMat = CreateFlatColorMaterial(new Color(0.55f, 0.04f, 0.04f), 0.35f);
        Material boneMat = CreateFlatColorMaterial(new Color(0.85f, 0.83f, 0.74f), 0.1f);

        // 1. Dark Basalt Floor (Collider removed so the player walks seamlessly on terrain)
        GameObject caveFloor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        caveFloor.name = "Cave_Basalt_Floor";
        caveFloor.transform.SetParent(caveRoot.transform);
        caveFloor.transform.localPosition = new Vector3(0f, 0.01f, 0f);
        caveFloor.transform.localScale = new Vector3(28f, 0.02f, 28f);
        caveFloor.GetComponent<Renderer>().sharedMaterial = darkRockMat;
        StripCollider(caveFloor);

        // 2. High Ceiling Dome (NO COLLIDER - Zero collision ceiling)
        GameObject caveRoof = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        caveRoof.name = "Cave_Ceiling_Arch";
        caveRoof.transform.SetParent(caveRoot.transform);
        caveRoof.transform.localPosition = new Vector3(0f, 9.5f, 0f);
        caveRoof.transform.localScale = new Vector3(30f, 9.5f, 30f);
        caveRoof.GetComponent<Renderer>().sharedMaterial = darkRockMat;
        StripCollider(caveRoof);

        // 3. Perimeter Walls with a HUGE 120-DEGREE OPEN WALK-IN ENTRANCE (150° to 270°)
        int wallSegments = 24;
        float radius = 13.5f;
        for (int i = 0; i < wallSegments; i++)
        {
            float angle = (360f / wallSegments) * i;

            // Wide open entrance facing straight towards map origin (0, 0)
            if (angle >= 145f && angle <= 275f) continue;

            float rad = angle * Mathf.Deg2Rad;
            Vector3 wallPos = new Vector3(Mathf.Cos(rad) * radius, 4.5f, Mathf.Sin(rad) * radius);

            GameObject wallRock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallRock.name = "Cave_RockWall_" + i;
            wallRock.transform.SetParent(caveRoot.transform);
            wallRock.transform.localPosition = wallPos;
            wallRock.transform.localRotation = Quaternion.Euler(Random.Range(-8f, 8f), angle, Random.Range(-6f, 6f));
            wallRock.transform.localScale = new Vector3(4.8f, 9.5f, 4.8f);
            wallRock.GetComponent<Renderer>().sharedMaterial = darkRockMat;
        }

        // Entrance Portal Framing Pillars (Colliders stripped to guarantee no getting stuck)
        float[] entranceAngles = { 140f, 280f };
        for (int e = 0; e < entranceAngles.Length; e++)
        {
            float rad = entranceAngles[e] * Mathf.Deg2Rad;
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "Cave_Entrance_Pillar_" + e;
            pillar.transform.SetParent(caveRoot.transform);
            pillar.transform.localPosition = new Vector3(Mathf.Cos(rad) * radius, 4.0f, Mathf.Sin(rad) * radius);
            pillar.transform.localScale = new Vector3(2.5f, 4.5f, 2.5f);
            pillar.GetComponent<Renderer>().sharedMaterial = darkRockMat;
            StripCollider(pillar);
        }

        // 4. Sharp Hanging Ceiling Stalactites (Colliders stripped)
        for (int s = 0; s < 16; s++)
        {
            float sAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float sDist = Random.Range(2.5f, 11f);
            Vector3 stalactitePos = new Vector3(Mathf.Cos(sAngle) * sDist, Random.Range(6.5f, 8.2f), Mathf.Sin(sAngle) * sDist);

            GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spike.name = "Ceiling_Stalactite_" + s;
            spike.transform.SetParent(caveRoot.transform);
            spike.transform.localPosition = stalactitePos;
            spike.transform.localScale = new Vector3(0.5f, Random.Range(2.0f, 3.5f), 0.5f);
            spike.transform.localRotation = Quaternion.Euler(Random.Range(-12f, 12f), 0f, Random.Range(-12f, 12f));
            spike.GetComponent<Renderer>().sharedMaterial = darkRockMat;
            StripCollider(spike);
        }

        // 5. Scary Blood Altar (Rear center of cave)
        GameObject bloodAltar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bloodAltar.name = "Scary_Blood_Altar";
        bloodAltar.transform.SetParent(caveRoot.transform);
        bloodAltar.transform.localPosition = new Vector3(0f, 0.45f, 8.5f);
        bloodAltar.transform.localScale = new Vector3(3.5f, 0.9f, 2.4f);
        bloodAltar.GetComponent<Renderer>().sharedMaterial = bloodMat;

        // Blood Splatter Pools
        for (int p = 0; p < 4; p++)
        {
            GameObject bloodPool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bloodPool.name = "Blood_Splatter_" + p;
            bloodPool.transform.SetParent(caveRoot.transform);
            bloodPool.transform.localPosition = new Vector3(Random.Range(-3.0f, 3.0f), 0.035f, 8.5f + Random.Range(-2.2f, 2.2f));
            bloodPool.transform.localScale = new Vector3(Random.Range(1.4f, 2.8f), 0.01f, Random.Range(1.4f, 2.8f));
            bloodPool.GetComponent<Renderer>().sharedMaterial = bloodMat;
            StripCollider(bloodPool);
        }

        // 6. Scattered Victim Bones
        for (int b = 0; b < 20; b++)
        {
            float bAngle = Random.Range(-30f, 150f) * Mathf.Deg2Rad;
            float bDist = Random.Range(4.0f, 10.5f);

            GameObject bone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bone.name = "Victim_Bone_" + b;
            bone.transform.SetParent(caveRoot.transform);
            bone.transform.localPosition = new Vector3(Mathf.Cos(bAngle) * bDist, 0.12f, Mathf.Sin(bAngle) * bDist);
            bone.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 85f);
            bone.transform.localScale = new Vector3(0.12f, Random.Range(0.45f, 0.9f), 0.12f);
            bone.GetComponent<Renderer>().sharedMaterial = boneMat;
            StripCollider(bone);
        }

        // 7. Eerie Cavern Core Light (Atmospheric dim red glow)
        GameObject scaryLightObj = new GameObject("Cavern_Eerie_Light");
        scaryLightObj.transform.SetParent(caveRoot.transform);
        scaryLightObj.transform.localPosition = new Vector3(0f, 4.0f, 2.5f);
        Light caveLight = scaryLightObj.AddComponent<Light>();
        caveLight.type = LightType.Point;
        caveLight.color = new Color(0.95f, 0.12f, 0.08f);
        caveLight.intensity = 2.0f;
        caveLight.range = 28f;

        // 8. Spawn the Cave Monster inside
        SpawnCaveMonster(caveRoot);
    }

    private void StripCollider(GameObject obj)
    {
        Collider c = obj.GetComponent<Collider>();
        if (c != null)
        {
            if (Application.isPlaying) Destroy(c);
            else DestroyImmediate(c);
        }
    }

    private void SpawnCaveMonster(GameObject caveRoot)
    {
        GameObject monsterObj = new GameObject("Cave_Monster");
        monsterObj.transform.SetParent(caveRoot.transform);
        monsterObj.transform.localPosition = new Vector3(0f, 1.2f, 2.0f);

        ProceduralWildlife monsterAI = monsterObj.AddComponent<ProceduralWildlife>();
        monsterAI.species = AnimalSpecies.CaveMonster;
        monsterAI.isBoundToCave = true;
        monsterAI.caveCenter = caveRoot.transform.position;
        monsterAI.caveRadius = 10.0f;
        monsterAI.triggerDistance = 14.0f; // Roars as soon as player enters the cavern

        Material monsterSkin = CreateFlatColorMaterial(new Color(0.06f, 0.05f, 0.07f), 0.05f);
        Material eyeGlow = CreateFlatColorMaterial(new Color(1.0f, 0.02f, 0.02f), 0.95f);

        // Torso
        GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
        torso.name = "Monster_Torso";
        torso.transform.SetParent(monsterObj.transform);
        torso.transform.localPosition = Vector3.zero;
        torso.transform.localScale = new Vector3(1.8f, 2.1f, 1.4f);
        torso.GetComponent<Renderer>().sharedMaterial = monsterSkin;

        // Head
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "Monster_Head";
        head.transform.SetParent(monsterObj.transform);
        head.transform.localPosition = new Vector3(0f, 1.4f, 0.55f);
        head.transform.localScale = new Vector3(1.3f, 1.1f, 1.3f);
        head.GetComponent<Renderer>().sharedMaterial = monsterSkin;

        // Glowing Red Eyes
        GameObject eyeL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eyeL.transform.SetParent(monsterObj.transform);
        eyeL.transform.localPosition = new Vector3(-0.38f, 1.5f, 1.22f);
        eyeL.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
        eyeL.GetComponent<Renderer>().sharedMaterial = eyeGlow;

        GameObject eyeR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eyeR.transform.SetParent(monsterObj.transform);
        eyeR.transform.localPosition = new Vector3(0.38f, 1.5f, 1.22f);
        eyeR.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
        eyeR.GetComponent<Renderer>().sharedMaterial = eyeGlow;

        // Claws
        GameObject armL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        armL.transform.SetParent(monsterObj.transform);
        armL.transform.localPosition = new Vector3(-1.35f, -0.2f, 0.25f);
        armL.transform.localScale = new Vector3(0.6f, 1.9f, 0.7f);
        armL.GetComponent<Renderer>().sharedMaterial = monsterSkin;

        GameObject armR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        armR.transform.SetParent(monsterObj.transform);
        armR.transform.localPosition = new Vector3(1.35f, -0.2f, 0.25f);
        armR.transform.localScale = new Vector3(0.6f, 1.9f, 0.7f);
        armR.GetComponent<Renderer>().sharedMaterial = monsterSkin;
    }

    private void BuildSeasonalRiver(GameObject parent, Season season, System.Random rnd)
    {
        GameObject riverGroup = new GameObject("River_System");
        riverGroup.transform.SetParent(parent.transform);
        riverGroup.transform.localPosition = Vector3.zero;

        float riverWidth = 7.0f;
        float riverElevation = 0.08f;
        Color waterColor = new Color(0.15f, 0.45f, 0.85f, 0.85f);
        float smoothness = 0.95f;

        switch (season)
        {
            case Season.Winter:
                waterColor = new Color(0.85f, 0.95f, 1.0f, 0.95f);
                riverWidth = 7.2f;
                riverElevation = 0.12f;
                smoothness = 0.98f;
                break;
            case Season.Summer:
                waterColor = new Color(0.18f, 0.55f, 0.75f, 0.75f);
                riverWidth = 4.2f;
                riverElevation = 0.04f;
                smoothness = 0.92f;
                break;
            case Season.Spring:
                waterColor = new Color(0.20f, 0.50f, 0.80f, 0.88f);
                riverWidth = 8.0f;
                riverElevation = 0.10f;
                smoothness = 0.96f;
                break;
            case Season.Autumn:
                waterColor = new Color(0.35f, 0.45f, 0.55f, 0.85f);
                riverWidth = 6.5f;
                riverElevation = 0.07f;
                smoothness = 0.90f;
                break;
        }

        GameObject waterSurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        waterSurface.name = (season == Season.Winter) ? "Frozen_Ice_River" : "Flowing_River_Water";
        waterSurface.transform.SetParent(riverGroup.transform);
        waterSurface.transform.localPosition = new Vector3(0f, riverElevation, 0f);
        waterSurface.transform.localScale = new Vector3(riverWidth, 0.1f, 100f);

        Material waterMat = CreateFlatColorMaterial(waterColor, smoothness);
        waterSurface.GetComponent<Renderer>().sharedMaterial = waterMat;

        Material bankStoneMat = CreateFlatColorMaterial(new Color(0.38f, 0.38f, 0.40f), 0.2f);
        int stoneCount = (season == Season.Summer) ? 65 : 35;

        for (int s = 0; s < stoneCount; s++)
        {
            float sz = (float)(rnd.NextDouble() * 96.0 - 48.0);
            float side = (s % 2 == 0) ? -1f : 1f;
            float sx = (riverWidth * 0.5f * side) + (float)(rnd.NextDouble() * 1.8 - 0.9);

            GameObject riverStone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            riverStone.name = "River_Stone";
            riverStone.transform.SetParent(riverGroup.transform);
            float rockSize = (float)(rnd.NextDouble() * 0.4 + 0.3);
            riverStone.transform.localPosition = new Vector3(sx, 0.15f, sz);
            riverStone.transform.localScale = new Vector3(rockSize * 1.2f, rockSize * 0.5f, rockSize * 1.2f);
            riverStone.GetComponent<Renderer>().sharedMaterial = bankStoneMat;
        }

        ProceduralRiverAudio riverAudio = riverGroup.AddComponent<ProceduralRiverAudio>();
        riverAudio.soundstage = soundstage;

        if (season != Season.Winter)
        {
            SpawnAquaticRiverLife(riverGroup, season, riverElevation);
        }
    }

    private void SpawnAquaticRiverLife(GameObject riverGroup, Season season, float waterY)
    {
        if (season == Season.Spring || season == Season.Summer)
        {
            int fishCount = (season == Season.Spring) ? 12 : 8;
            for (int f = 0; f < fishCount; f++)
            {
                GameObject fishObj = new GameObject("River_Fish_" + f);
                fishObj.transform.SetParent(riverGroup.transform);
                float fx = Random.Range(-1.8f, 1.8f);
                float fz = Random.Range(-40f, 40f);
                fishObj.transform.position = new Vector3(fx, waterY - 0.02f, fz);

                ProceduralWildlife fishAI = fishObj.AddComponent<ProceduralWildlife>();
                fishAI.species = AnimalSpecies.RiverFish;
                fishAI.associatedSeason = season;
                fishAI.triggerDistance = 4.0f;

                BuildFishMesh(fishObj, season);
            }
        }

        if (season == Season.Summer || season == Season.Autumn)
        {
            int crocCount = (season == Season.Summer) ? 3 : 2;
            for (int c = 0; c < crocCount; c++)
            {
                GameObject crocObj = new GameObject("River_Croc_" + c);
                crocObj.transform.SetParent(riverGroup.transform);
                float cx = Random.Range(-1.4f, 1.4f);
                float cz = Random.Range(-35f, 35f);
                crocObj.transform.position = new Vector3(cx, waterY + 0.05f, cz);

                ProceduralWildlife crocAI = crocObj.AddComponent<ProceduralWildlife>();
                crocAI.species = AnimalSpecies.RiverCrocodile;
                crocAI.associatedSeason = season;
                crocAI.triggerDistance = 5.5f;

                BuildCrocodileMesh(crocObj);
            }
        }
    }

    private void BuildFishMesh(GameObject root, Season season)
    {
        Color fishColor = (season == Season.Spring) ? new Color(1.0f, 0.45f, 0.15f) : new Color(0.2f, 0.75f, 0.95f);
        Material fishMat = CreateFlatColorMaterial(fishColor, 0.4f);
        Material finMat = CreateFlatColorMaterial(Color.white, 0.3f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.14f, 0.16f, 0.55f);
        body.GetComponent<Renderer>().sharedMaterial = fishMat;

        GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tail.transform.SetParent(root.transform);
        tail.transform.localPosition = new Vector3(0f, 0f, -0.32f);
        tail.transform.localScale = new Vector3(0.04f, 0.22f, 0.18f);
        tail.GetComponent<Renderer>().sharedMaterial = finMat;
    }

    private void BuildCrocodileMesh(GameObject root)
    {
        Material crocSkin = CreateFlatColorMaterial(new Color(0.18f, 0.28f, 0.14f), 0.1f);
        Material eyeMat = CreateFlatColorMaterial(new Color(0.9f, 0.8f, 0.1f), 0.8f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Croc_Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.7f, 0.3f, 2.2f);
        body.GetComponent<Renderer>().sharedMaterial = crocSkin;

        GameObject snout = GameObject.CreatePrimitive(PrimitiveType.Cube);
        snout.name = "Croc_Snout";
        snout.transform.SetParent(root.transform);
        snout.transform.localPosition = new Vector3(0f, -0.04f, 1.4f);
        snout.transform.localScale = new Vector3(0.5f, 0.2f, 0.9f);
        snout.GetComponent<Renderer>().sharedMaterial = crocSkin;

        GameObject eyeR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eyeR.transform.SetParent(root.transform);
        eyeR.transform.localPosition = new Vector3(0.2f, 0.18f, 0.95f);
        eyeR.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
        eyeR.GetComponent<Renderer>().sharedMaterial = eyeMat;

        GameObject eyeL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eyeL.transform.SetParent(root.transform);
        eyeL.transform.localPosition = new Vector3(-0.2f, 0.18f, 0.95f);
        eyeL.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
        eyeL.GetComponent<Renderer>().sharedMaterial = eyeMat;

        GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tail.transform.SetParent(root.transform);
        tail.transform.localPosition = new Vector3(0f, 0f, -1.6f);
        tail.transform.localScale = new Vector3(0.35f, 0.22f, 1.2f);
        tail.GetComponent<Renderer>().sharedMaterial = crocSkin;
    }

    private ParticleSystem CreateRiverSplashParticles(string name, Transform parent)
    {
        GameObject pObj = new GameObject(name);
        pObj.transform.SetParent(parent);
        pObj.transform.localPosition = new Vector3(0f, 0.18f, 0f);

        ParticleSystem ps = pObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 0.45f;
        main.startSpeed = 0.5f;
        main.startSize = 0.45f;
        main.startColor = new Color(0.9f, 0.95f, 1.0f, 0.8f);
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 40;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(7.5f, 0.05f, 96f);
        shape.rotation = new Vector3(0f, 0f, 0f);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 0.2f);
        curve.AddKey(1.0f, 1.4f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        var renderer = pObj.GetComponent<ParticleSystemRenderer>();
        Shader pShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                      ?? Shader.Find("Particles/Standard Unlit")
                      ?? Shader.Find("Sprites/Default");
        Material pMat = new Material(pShader);
        renderer.sharedMaterial = pMat;

        ps.Play();
        return ps;
    }

    private void PopulateUltraDenseGrass(GameObject parent, Season season, System.Random rnd)
    {
        int tuftCount = (season == Season.Winter) ? 200 : (season == Season.Autumn ? 550 : 900);

        Color grassColor;
        switch (season)
        {
            case Season.Spring: grassColor = new Color(0.24f, 0.62f, 0.20f); break;
            case Season.Summer: grassColor = new Color(0.18f, 0.50f, 0.14f); break;
            case Season.Autumn: grassColor = new Color(0.55f, 0.40f, 0.16f); break;
            case Season.Winter:
            default: grassColor = new Color(0.60f, 0.70f, 0.78f); break;
        }

        Material grassMat = CreateFlatColorMaterial(grassColor, 0.05f);
        GameObject grassContainer = new GameObject("Dense_Grass_System");
        grassContainer.transform.SetParent(parent.transform);

        int batches = 4;
        int tuftsPerBatch = tuftCount / batches;

        for (int b = 0; b < batches; b++)
        {
            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            List<Vector3> norms = new List<Vector3>();

            for (int t = 0; t < tuftsPerBatch; t++)
            {
                float gx = (float)(rnd.NextDouble() * 88.0 - 44.0);
                float gz = (float)(rnd.NextDouble() * 88.0 - 44.0);

                if (Mathf.Abs(gx) < 6.0f) continue;
                if (Vector2.Distance(new Vector2(gx, gz), new Vector2(30f, 25f)) < 16.0f) continue;
                if (Mathf.Abs(gx) < 2.5f && Mathf.Abs(gz) < 2.5f) continue;

                float yaw = (float)(rnd.NextDouble() * 360.0);
                float scaleH = (float)(rnd.NextDouble() * 0.4 + 0.8);
                float scaleW = (float)(rnd.NextDouble() * 0.3 + 0.9);

                AddDenseTuftVertices(verts, tris, norms, new Vector3(gx, 0f, gz), yaw, scaleW, scaleH);
            }

            GameObject batchObj = new GameObject("Grass_Chunk_" + b);
            batchObj.transform.SetParent(grassContainer.transform);
            batchObj.transform.localPosition = Vector3.zero;

            Mesh chunkMesh = new Mesh();
            chunkMesh.name = "GrassChunk_" + b;
            chunkMesh.SetVertices(verts);
            chunkMesh.SetTriangles(tris, 0);
            chunkMesh.SetNormals(norms);
            chunkMesh.RecalculateBounds();

            MeshFilter mf = batchObj.AddComponent<MeshFilter>();
            MeshRenderer mr = batchObj.AddComponent<MeshRenderer>();
            mf.sharedMesh = chunkMesh;
            mr.sharedMaterial = grassMat;
        }
    }

    private void AddDenseTuftVertices(List<Vector3> verts, List<int> tris, List<Vector3> norms, Vector3 center, float baseAngle, float scaleW, float scaleH)
    {
        float[] angles = { 0f, 25f, 45f, 70f, 90f, 115f, 135f, 160f };
        float bladeWidth = 0.07f * scaleW;
        float bladeHeight = 0.65f * scaleH;

        for (int i = 0; i < angles.Length; i++)
        {
            int vStart = verts.Count;
            float rad = (angles[i] + baseAngle) * Mathf.Deg2Rad;

            Vector3 right = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * bladeWidth;
            Vector3 tipLean = new Vector3(Mathf.Sin(rad), 0f, -Mathf.Cos(rad)) * (0.08f * (i % 2 == 0 ? 1f : -1f));

            Vector3 v0 = center - right;
            Vector3 v1 = center + right;
            Vector3 v2 = center + tipLean + Vector3.up * bladeHeight;

            verts.Add(v0); verts.Add(v1); verts.Add(v2);
            verts.Add(v0); verts.Add(v1); verts.Add(v2);

            for (int n = 0; n < 6; n++) norms.Add(Vector3.up);

            tris.Add(vStart + 0); tris.Add(vStart + 2); tris.Add(vStart + 1);
            tris.Add(vStart + 3); tris.Add(vStart + 4); tris.Add(vStart + 5);
        }
    }

    private void SpawnSeasonalWildlife(GameObject parent, Season season)
    {
        int animalCount = (season == Season.Summer) ? 10 : 5;

        for (int i = 0; i < animalCount; i++)
        {
            float rx = Random.Range(-36f, 36f);
            float rz = Random.Range(-36f, 36f);
            if (Mathf.Abs(rx) < 6f && Mathf.Abs(rz) < 6f) continue;
            if (Vector2.Distance(new Vector2(rx, rz), new Vector2(30f, 25f)) < 16.0f) continue;

            GameObject animalObj = new GameObject(season.ToString() + "_Animal_" + i);
            animalObj.transform.SetParent(parent.transform);

            ProceduralWildlife wildlife = animalObj.AddComponent<ProceduralWildlife>();
            wildlife.associatedSeason = season;

            switch (season)
            {
                case Season.Winter:
                    wildlife.species = AnimalSpecies.ArcticWolf;
                    wildlife.triggerDistance = 7.5f;
                    animalObj.transform.position = new Vector3(rx, 0.55f, rz);
                    BuildWolfMesh(animalObj);
                    break;

                case Season.Summer:
                    wildlife.species = AnimalSpecies.SummerCicada;
                    wildlife.triggerDistance = 4.5f;
                    animalObj.transform.position = new Vector3(rx, 1.8f, rz);
                    BuildCicadaMesh(animalObj);
                    break;

                case Season.Spring:
                    wildlife.species = AnimalSpecies.Songbird;
                    wildlife.triggerDistance = 5.0f;
                    animalObj.transform.position = new Vector3(rx, 0.4f, rz);
                    BuildBirdMesh(animalObj);
                    break;

                case Season.Autumn:
                    wildlife.species = AnimalSpecies.WoodlandElk;
                    wildlife.triggerDistance = 8.5f;
                    animalObj.transform.position = new Vector3(rx, 0.9f, rz);
                    BuildElkMesh(animalObj);
                    break;
            }
        }
    }

    private void BuildWolfMesh(GameObject root)
    {
        Material wolfMat = CreateFlatColorMaterial(new Color(0.88f, 0.92f, 0.96f), 0.1f);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.6f, 0.6f, 1.3f);
        body.GetComponent<Renderer>().sharedMaterial = wolfMat;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.transform.SetParent(root.transform);
        head.transform.localPosition = new Vector3(0f, 0.35f, 0.75f);
        head.transform.localScale = new Vector3(0.4f, 0.4f, 0.5f);
        head.GetComponent<Renderer>().sharedMaterial = wolfMat;
    }

    private void BuildCicadaMesh(GameObject root)
    {
        Material insectMat = CreateFlatColorMaterial(new Color(0.15f, 0.25f, 0.12f), 0.5f);
        GameObject bug = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bug.transform.SetParent(root.transform);
        bug.transform.localPosition = Vector3.zero;
        bug.transform.localScale = new Vector3(0.2f, 0.15f, 0.35f);
        bug.GetComponent<Renderer>().sharedMaterial = insectMat;
    }

    private void BuildBirdMesh(GameObject root)
    {
        Material featherMat = CreateFlatColorMaterial(new Color(0.2f, 0.7f, 0.9f), 0.15f);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.35f, 0.35f, 0.5f);
        body.GetComponent<Renderer>().sharedMaterial = featherMat;
    }

    private void BuildElkMesh(GameObject root)
    {
        Material elkMat = CreateFlatColorMaterial(new Color(0.42f, 0.25f, 0.14f), 0.05f);
        Material antlerMat = CreateFlatColorMaterial(new Color(0.7f, 0.65f, 0.55f), 0.0f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.85f, 0.9f, 1.8f);
        body.GetComponent<Renderer>().sharedMaterial = elkMat;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.transform.SetParent(root.transform);
        head.transform.localPosition = new Vector3(0f, 0.65f, 1.0f);
        head.transform.localScale = new Vector3(0.45f, 0.55f, 0.6f);
        head.GetComponent<Renderer>().sharedMaterial = elkMat;

        GameObject antlers = GameObject.CreatePrimitive(PrimitiveType.Cube);
        antlers.transform.SetParent(root.transform);
        antlers.transform.localPosition = new Vector3(0f, 1.15f, 0.95f);
        antlers.transform.localScale = new Vector3(1.6f, 0.45f, 0.2f);
        antlers.GetComponent<Renderer>().sharedMaterial = antlerMat;
    }

    private ParticleSystem CreateWeatherParticles(string name, Transform parent, Color tint, int maxParticles, float speed, float size, bool isStretched)
    {
        GameObject pObj = new GameObject(name);
        pObj.transform.SetParent(parent);
        pObj.transform.localPosition = new Vector3(0f, 15f, 0f);

        ParticleSystem ps = pObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 4.5f;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = tint;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = maxParticles / 3;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(50f, 50f, 1f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        var renderer = pObj.GetComponent<ParticleSystemRenderer>();
        if (isStretched)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.25f;
            renderer.lengthScale = 2.0f;
        }

        Shader pShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                      ?? Shader.Find("Particles/Standard Unlit")
                      ?? Shader.Find("Sprites/Default");
        Material pMat = new Material(pShader);
        renderer.sharedMaterial = pMat;

        ps.Stop();
        return ps;
    }

    private Material CreateFlatColorMaterial(Color color, float smoothness)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default");
        Material mat = new Material(s);
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        return mat;
    }

    public void ApplySeasonVisuals(Season season)
    {
        lastSeason = season;

        if (winterRoot) winterRoot.SetActive(season == Season.Winter);
        if (summerRoot) summerRoot.SetActive(season == Season.Summer);
        if (springRoot) springRoot.SetActive(season == Season.Spring);
        if (autumnRoot) autumnRoot.SetActive(season == Season.Autumn);

        ToggleParticleSystem(snowEffect, season == Season.Winter);
        ToggleParticleSystem(rainEffect, season == Season.Spring);
        ToggleParticleSystem(leavesEffect, season == Season.Autumn);
        ToggleParticleSystem(springRiverSplashes, season == Season.Spring);

        switch (season)
        {
            case Season.Winter:
                SetLightingAndAtmosphere(
                    new Color(0.78f, 0.88f, 1.0f), 0.65f, Quaternion.Euler(22f, -30f, 0f),
                    new Color(0.85f, 0.92f, 0.98f), 0.032f, new Color(0.65f, 0.75f, 0.85f)
                );
                break;

            case Season.Summer:
                SetLightingAndAtmosphere(
                    new Color(1.0f, 0.95f, 0.82f), 1.35f, Quaternion.Euler(65f, -40f, 0f),
                    new Color(0.92f, 0.90f, 0.82f), 0.007f, new Color(0.85f, 0.82f, 0.68f)
                );
                break;

            case Season.Spring:
                SetLightingAndAtmosphere(
                    new Color(0.88f, 0.92f, 0.85f), 0.85f, Quaternion.Euler(38f, -45f, 0f),
                    new Color(0.80f, 0.88f, 0.84f), 0.022f, new Color(0.70f, 0.80f, 0.75f)
                );
                break;

            case Season.Autumn:
                SetLightingAndAtmosphere(
                    new Color(1.0f, 0.65f, 0.38f), 0.95f, Quaternion.Euler(18f, -70f, 0f),
                    new Color(0.85f, 0.62f, 0.48f), 0.018f, new Color(0.80f, 0.55f, 0.40f)
                );
                break;
        }
    }

    private void SetLightingAndAtmosphere(Color sunColor, float sunIntensity, Quaternion sunRotation, Color fogColor, float fogDensity, Color ambientColor)
    {
        if (directionalSun != null)
        {
            directionalSun.color = sunColor;
            directionalSun.intensity = sunIntensity;
            directionalSun.transform.rotation = sunRotation;
        }
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.ambientLight = ambientColor;
    }

    private void ToggleParticleSystem(ParticleSystem ps, bool active)
    {
        if (ps == null) return;
        if (active)
        {
            if (!ps.isPlaying) ps.Play();
        }
        else
        {
            if (ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}