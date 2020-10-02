using Fasteroids.DataLayer;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.SocialPlatforms.Impl;
using UnityEditor;

public class MyComparer : Comparer<AsteroidDto>
{
    // Compares by Length, Height, and Width.
    public override int Compare( AsteroidDto x, AsteroidDto y )
    {
        if ( x.Position.x < y.Position.x)
            return -1;

        if ( x.Position.x == y.Position.x ) 
            return 0;

        return 1;
    }
}

public class GameLogic : MonoBehaviour
{
    [StructLayout(LayoutKind.Explicit)]
    struct FloatIntUnion
    {
        [FieldOffset(0)]
        public float f;

        [FieldOffset(0)]
        public int tmp;
    }

    #region Constants
    float AsteroidRadius = 0.20f;
    float AsteroidTranformValueZ = 0.4f;

    const int GridDimensionInt = 160;
    const float GridDimensionFloat = 160;
    const int TotalNumberOfAsteroids = GridDimensionInt * GridDimensionInt;
    const int MaxLaserBeams = 20;

    public string ShipName;

    // pool sizes
    const int AsteroidPoolSize = 40; // in tests this never went above 35 so for safety I gave 5 more

    float FrustumSizeX = 3.8f;
    float FrustumSizeY = 2.3f;
    #endregion

    #region Private Fields
    // readonly fields and tables
    public static readonly AsteroidDto[] _asteroids = new AsteroidDto[TotalNumberOfAsteroids];
    public static readonly LaserDto[] _laserBeams = new LaserDto[MaxLaserBeams];

    // this is were unused object goes upon death
    static readonly Vector3 _objectGraveyardPosition = new Vector3(-99999, -99999, 0.3f);

    // prefabs
    [SerializeField] GameObject _asteroidPrefab;
    [SerializeField] GameObject _spaceshipPrefab;
    [SerializeField] GameObject _laserPrefab;

    // other references
    [SerializeField] Camera _mainCamera;
    [SerializeField] Button _restartButton;
    [SerializeField] Text _youLoseLabel;
    [SerializeField] Text _spaceshipname;
    [SerializeField] Text _score;
    [SerializeField] Text _highScore;


    // object pools
    GameObject[] _laserPool = new GameObject[MaxLaserBeams];
    Vector3 _playerCachedPosition;

    Transform _playerTransform;
    bool _playerDestroyed;
    #endregion


    public int newAsteroids;
    public GameObject[] _asteroidPool = new GameObject[AsteroidPoolSize];
    public Vector2[] newAsteroidsV2;
    public float[] newAsteroidsRadian;
    public int highScore;
    public bool nameEntered = true;
    IRepository spaceshipRepository = new SpaceshipRepository();


    void Start()
    {
       
            for (int i = 0, j = 0; i < AsteroidPoolSize; i++)
            {
                if (_asteroidPool[i] != null)
                {
                    newAsteroids++;
                }
            }
            checkEditor();
            _restartButton.gameObject.SetActive(false);
            _youLoseLabel.gameObject.SetActive(false);
            CreateObjectPoolsAndTables();
            InitializeAsteroidsGridLayout();
            System.Array.Sort(_asteroids, new MyComparer());

            _playerTransform = Instantiate(_spaceshipPrefab).transform;
            _playerTransform.position = new Vector3(
                GridDimensionFloat / 2f - 0.5f,
                GridDimensionFloat / 2f - 0.5f,
                0.3f);
            SpaceshipInfo ship = spaceshipRepository.LoadData();
            spaceshipRepository.SaveData(_spaceshipname.text, ship.highScore);
            ShipName = _spaceshipname.text;
        
    }

    void Update()
    {
        if (nameEntered)
        {
            _playerCachedPosition = _playerTransform.position;

            HandleInput();

            UpdateAsteroids();
            System.Array.Sort(_asteroids, new MyComparer());

            CheckCollisionsBetweenAsteroids();
            CheckCollisionsWithLaser();
            CheckCollisionsWithShip();
            LaserVisible();
            ShowVisibleAsteroids();

            if (!_playerDestroyed)
                _mainCamera.transform.position = new Vector3(_playerTransform.position.x, _playerTransform.position.y, 0f);
        }
        
    }

    int laserCount;
    float playerRadius = 0.08f;
    float laserRadius = 0.07f;
    float playerSpeed = 1.25f;
    float tme;
    public int points;

    void HandleInput()
    {
        if (!_playerDestroyed)
        {
            ///// Keyboard
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                _playerTransform.position += _playerTransform.up * Time.deltaTime * playerSpeed;
            else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                _playerTransform.position -= _playerTransform.up * Time.deltaTime * playerSpeed;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                _playerTransform.Rotate(new Vector3(0, 0, 3f));
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                _playerTransform.Rotate(new Vector3(0, 0, -3f));


            ////// Laser
            if(Input.GetKeyDown(KeyCode.Space))
            {   
                _laserBeams[laserCount] = new LaserDto()
                {
                    Position = _playerTransform.position,
                    Direction = _playerTransform.up,
                    Speed = 0.1f
                };
                laserCount++;
                if (laserCount >= MaxLaserBeams - 1)
                    laserCount = 0;
            }

            ////// MOUSE
            Vector2 a = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - (_playerTransform.position));
            Vector2 b = (_playerTransform.up);
            float angle = Vector2.SignedAngle(a, b);
            if (Input.GetMouseButton(0))
            {
                _playerTransform.position += _playerTransform.up * Time.deltaTime * playerSpeed;
            }
            if (Input.GetMouseButton(1))
            {
                _playerTransform.position -= _playerTransform.up * Time.deltaTime * playerSpeed;
            }
            if (Input.GetMouseButton(0) && angle < -5)
            {
                _playerTransform.Rotate(new Vector3(0, 0, 3f));
            }
            if (Input.GetMouseButton(0) && angle > 5)
            {
                _playerTransform.Rotate(new Vector3(0, 0, -3f));
            }
            if (Input.GetMouseButton(1) && angle < -5)
            {
                _playerTransform.Rotate(new Vector3(0, 0, -3f));
            }
            if (Input.GetMouseButton(1) && angle > 5)
            {
                _playerTransform.Rotate(new Vector3(0, 0, 3f));
            }
            if(Input.GetMouseButton(0) && Input.GetMouseButton(1) && Input.GetMouseButton(1) && angle < -5)
            {
                _playerTransform.Rotate(new Vector3(0, 0, 3f));
            }
            if (Input.GetMouseButton(0) && Input.GetMouseButton(1) && Input.GetMouseButton(1) && angle > 5)
            {
                _playerTransform.Rotate(new Vector3(0, 0, -3f));
            }
        }

        if (Input.GetKey(KeyCode.Escape))
            Application.Quit();
    }

    /// <summary>
    /// Updates asteroids' position or respawns them when time comes.
    /// </summary>
    void UpdateAsteroids()
    {
        float deltaTime = Time.deltaTime;
        for (int i = 0; i < TotalNumberOfAsteroids; ++i)
        {
            ref AsteroidDto a = ref _asteroids[i];

            if (a.DestroyedThisFrame == false)
            {
                a.Position += a.Direction * a.Speed;

                continue;
            }

            a.TimeLeftToRespawn -= deltaTime;
            if (a.TimeLeftToRespawn <= 0)
                RespawnAsteroid(ref a);
        }
        for (int i = 0; i < MaxLaserBeams; ++i)                         ////// Updates lasers' position as well
        {
            ref LaserDto a = ref _laserBeams[i];
             a.Position += a.Direction * a.Speed;
            continue;
        }
    }

    void RespawnAsteroid(ref AsteroidDto a)
    {
        // iterate until you find a position outside of player's frustum
        // it is not the most mathematically correct solution
        // as the asteroids dispersion will not be even (those that normally would spawn inside the frustum 
        // will spawn right next to the frustum's edge instead)
        float posX = UnityEngine.Random.Range(0, GridDimensionFloat);
        if (posX > _playerTransform.position.x)
        {
            // tried to spawn on the right side of the player
            float value1 = posX;
            if (value1 < 0)
                value1 *= -1;

            float value2 = _playerTransform.position.x;
            if (value2 < 0)
                value2 *= -1;

            if (value1 - value2 < FrustumSizeX)
                posX += FrustumSizeX;
        }
        else
        {
            // left side
            float value1 = posX;
            if (value1 < 0)
                value1 *= -1;

            float value2 = _playerTransform.position.x;
            if (value2 < 0)
                value2 *= -1;

            if (value2 - value1 < FrustumSizeX)
                posX -= FrustumSizeX;
        }

        float posY = UnityEngine.Random.Range(0, GridDimensionFloat);
        if (posY > _playerTransform.position.y)
        {
            // tried to spawn above the player
            float value1 = posY;
            if (value1 < 0)
                value1 *= -1;

            float value2 = _playerTransform.position.y;
            if (value2 < 0)
                value2 *= -1;

            if (value1 - value2 < FrustumSizeY)
                posY += FrustumSizeY;
        }
        else
        {
            // below
            float value1 = posX;
            if (value1 < 0)
                value1 *= -1;

            float value2 = _playerTransform.position.y;
            if (value2 < 0)
                value2 *= -1;

            if (value2 - value1 < FrustumSizeY)
                posY -= FrustumSizeY;
        }

        // respawn
        a.Position = new Vector3(posX, posY, 0);
    }


    /// <summary>
    /// When laser hits an asteroid, the laser goes to possition (-100,0,0) (Lasers' graveyard) with speed 0,
    /// the asteroid is set to "DestroyedThisFrame = true" and score is increased by 1
    /// </summary>
    void CheckCollisionsWithLaser()
    {
        tme = tme + Time.deltaTime;
        if (tme > 1)
            for (int indexA = 0; indexA < TotalNumberOfAsteroids; indexA++)
            {
                ref AsteroidDto a = ref _asteroids[indexA];
                for (int indexB = 0; indexB < MaxLaserBeams; indexB++)
                {
                    ref LaserDto b = ref _laserBeams[indexB];
                    float difX = b.Position.x - a.Position.x;
                    if (difX < 0)
                        difX *= -1;
                    float difY = b.Position.y - a.Position.y;
                    if (difY < 0)
                        difY *= -1;
                    float distance = FastSqrt(difX * difX + difY * difY);
                    if (difX >= AsteroidRadius + laserRadius)
                    {
                        continue;
                    }
                    if (a.DestroyedThisFrame)
                        continue;

                    if (difX < AsteroidRadius + laserRadius)
                    {
                        if (difY >= AsteroidRadius + laserRadius)
                        {
                            continue;
                        }
                            if (difY < AsteroidRadius + laserRadius)
                        {
                            if (distance < AsteroidRadius + laserRadius && b.Position != new Vector3(0,0,0))
                            {
                                points++;
                                _score.text = "" + points;
                                a.DestroyedThisFrame = true;
                                b.Position = new Vector3(-100, 0, 0);
                                b.Speed = 0;
                                ++indexA;
                                break;
                            }
                        }
                    }
                }
            }
    }

    /// <summary>
    /// Check if there is any collision between any two asteroids in the game.
    /// Updates the game state if any collision has been found.
    /// </summary>
    void CheckCollisionsBetweenAsteroids()
    {
        // the last one is the last to the right it does not need to be processed because
        // its collisions are already handled by the ones preceding him
        for (int indexA = 0; indexA < TotalNumberOfAsteroids - 1; indexA++)
        {
            int indexB = indexA + 1;
            ref AsteroidDto a = ref _asteroids[indexA];
            ref AsteroidDto b = ref _asteroids[indexB];

            float difX = b.Position.x - a.Position.x;
            if (difX >= AsteroidRadius + AsteroidRadius)
                continue; // b is too far on x axis

            // a is destroyed
            if (a.DestroyedThisFrame)
                continue;

            // check for other asteroids
            while (indexB < TotalNumberOfAsteroids - 1)
            {
                float difY = b.Position.y - a.Position.y;
                if (difY < 0)
                    difY *= -1;

                if (difY >= AsteroidRadius + AsteroidRadius)
                {
                    b = ref _asteroids[++indexB];
                    difX = b.Position.x - a.Position.x;
                    if (difX >= AsteroidRadius + AsteroidRadius)
                        break; // b is too far on x axis
                    continue;
                }

                // b is destroyed
                if (b.DestroyedThisFrame)
                {
                    b = ref _asteroids[++indexB];
                    difX = b.Position.x - a.Position.x;
                    if (difX >= AsteroidRadius + AsteroidRadius)
                        break; // b is too far on x axis
                    continue;
                }

                float distance = FastSqrt(difX * difX + difY * difY);
                if (distance < AsteroidRadius + AsteroidRadius)
                {
                    // collision! mark both as destroyed in this frame and break the loop
                    a.DestroyedThisFrame = true; // destroyed
                    b.DestroyedThisFrame = true; // destroyed
                    a.TimeLeftToRespawn = 1f;
                    b.TimeLeftToRespawn = 1f;
                    ++indexA; // increase by one here and again in the for loop
                    break;
                }
                else
                {
                    // no collision with this one but it maybe with the next one
                    // as long as the x difference is lower than Radius * 2
                    b = ref _asteroids[++indexB];
                    difX = b.Position.x - a.Position.x;
                    if (difX >= AsteroidRadius + AsteroidRadius)
                        break; // b is too far on x axis
                }
            };
        }
    }

    /// <summary>
    /// Check if there is any collision between any asteroid and player.
    /// Updates the game state if any collision has been found.
    /// </summary>
    void CheckCollisionsWithShip()
    {
        float lowestX = _playerTransform.position.x;
        float highestX = _playerTransform.position.x;

        // find the range within collision is possible
        for (int i = 0; i < TotalNumberOfAsteroids; i++)
        {
            ref AsteroidDto a = ref _asteroids[i];

            // omit destroyed
            if (a.DestroyedThisFrame)
                continue;

            if (a.Position.x < lowestX)
            {
                float value = lowestX - a.Position.x;
                if (value < 0)
                    value *= -1;

                if (value > AsteroidRadius + playerRadius)
                    continue; // no collisions possible
            }
            else if (a.Position.x > highestX)
            {
                float value = highestX - a.Position.x;
                if (value < 0)
                    value *= -1;

                if (value > AsteroidRadius + playerRadius)
                    break; // no collisions possible neither for this nor for all the rest
            }

            if (_playerDestroyed)
                continue;

            // check collision with the player
            float distance = FastSqrt(
                (_playerTransform.position.x - a.Position.x) * (_playerTransform.position.x - a.Position.x)
                + (_playerTransform.position.y - a.Position.y) * (_playerTransform.position.y - a.Position.y));

            if (distance < AsteroidRadius + playerRadius)
            {
                // this asteroid destroyed player
                a.DestroyedThisFrame = true;
                GameOverFunction();
            }
        }
    }

    void GameOverFunction()
    {
        _playerDestroyed = true;
        _playerTransform.gameObject.SetActive(false);
        _restartButton.gameObject.SetActive(true);
        _youLoseLabel.gameObject.SetActive(true);
        HighScore();
        _highScore.gameObject.SetActive(true);
    }

    public void RestartGame()
    {
        _playerTransform.gameObject.SetActive(true);
        _playerTransform.position = new Vector3(
            GridDimensionFloat / 2f - 0.5f,
            GridDimensionFloat / 2f - 0.5f,
            0.3f);
        _playerTransform.rotation = new Quaternion(0, 0, 0, 0);

        _playerDestroyed = false;
        _restartButton.gameObject.SetActive(false);
        _youLoseLabel.gameObject.SetActive(false);
        _highScore.gameObject.SetActive(false);
        points = 0;
        _score.text = "" + points;

        InitializeAsteroidsGridLayout();
    }

    /// <summary>
    /// This function updates lasers' gameobject position and sends a lasers to a graveyard when they are not in the screen area
    /// </summary>
    void LaserVisible()
    {
        int poolElementIndexL = 0;
        for (int i = 0; i < MaxLaserBeams; i++)
        {
            ref LaserDto a = ref _laserBeams[i];
            _laserPool[poolElementIndexL++].gameObject.transform.position = new Vector3(a.Position.x,              ///Moving lasers' gameobject
                a.Position.y,
                AsteroidTranformValueZ);
            float value = _playerTransform.position.x - a.Position.x;
            if (value < 0)
                value *= -1;
            if (value > FrustumSizeX)
            { 
                _laserPool[poolElementIndexL - 1].transform.position = new Vector3(-100, 0, 0);
                a.Position = new Vector3(-100, 0, 0);
                a.Speed = 0;
            }
            value = _playerTransform.position.y - a.Position.y;
            if (value < 0)
                value *= -1;
            if (value > FrustumSizeY)
            {
                _laserPool[poolElementIndexL - 1].transform.position = new Vector3(-100, 0, 0);
                a.Position = new Vector3(-100, 0, 0);
                a.Speed = 0;
            }
     
        }

    }
    void ShowVisibleAsteroids()
    {
        int poolElementIndex = 0;

        for (int i = 0; i < TotalNumberOfAsteroids; i++)
        {
            ref AsteroidDto a = ref _asteroids[i];

            if (a.DestroyedThisFrame)
                continue;

            // is visible in x?
            float value = _playerTransform.position.x - a.Position.x;
            if (value < 0)
                value *= -1;
            if (value > FrustumSizeX)
                continue;

            // is visible in y?
            value = _playerTransform.position.y - a.Position.y;
            if (value < 0)
                value *= -1;
            if (value > FrustumSizeY)
                continue;

            // take first from the pool
            _asteroidPool[poolElementIndex++].gameObject.transform.position = new Vector3(
                a.Position.x,
                a.Position.y,
                AsteroidTranformValueZ);

        }

            // unused objects go to the graveyard
            while (poolElementIndex < AsteroidPoolSize)
            _asteroidPool[poolElementIndex++].transform.position = _objectGraveyardPosition;
    }

    #region Initializers
    void InitializeAsteroidsRandomPosition()
    {
        for (int x = 0, i = 0; x < GridDimensionInt; x++)
            for (int y = 0; y < GridDimensionInt; y++)
            {
                _asteroids[i++] = new AsteroidDto()
                {
                    Position = new Vector3(
                        UnityEngine.Random.Range(0, GridDimensionFloat),
                        UnityEngine.Random.Range(0, GridDimensionFloat),
                        0),
                    RotationSpeed = UnityEngine.Random.Range(0, 5f),
                    Direction = new Vector3(UnityEngine.Random.Range(-1, 1f), UnityEngine.Random.Range(-1, 1f), 0),
                    Speed = UnityEngine.Random.Range(0.01f, 0.05f),
                    DestroyedThisFrame = false
                };
            }
    }

    void InitializeAsteroidsGridLayout()
    {
        for (int x = 0, j = 0, i = 0; x < GridDimensionInt; x++)
            for (int y = 0; y < GridDimensionInt; y++)
            {
                if (i >= 0)
                {
                    _asteroids[i] = new AsteroidDto()
                    {
                        Position = new Vector3(x, y, 0),
                        RotationSpeed = UnityEngine.Random.Range(0, 5f),
                        Direction = new Vector3(UnityEngine.Random.Range(-1, 1f), UnityEngine.Random.Range(-1, 1f), 0),
                        Speed = UnityEngine.Random.Range(0.01f, 0.05f),
                        DestroyedThisFrame = false
                    };
                }

                ///All asteroids created by Asteroids Editor are being fit into the Grid
                if(j < newAsteroids && x >= newAsteroidsV2[j].x && newAsteroidsV2[j].x < x + 1 && y >= newAsteroidsV2[j].y && newAsteroidsV2[j].y < y + 1)
                {
                    _asteroids[i].Position = new Vector3(newAsteroidsV2[j].x, newAsteroidsV2[j].y, 0);
                    _asteroids[i].Direction = new Vector3((float)Math.Cos(newAsteroidsRadian[j]), (float)Math.Sin(newAsteroidsRadian[j]), 0);
                    j++;
                }
                i++;
            }
        
    }
    public void CreateObjectPoolsAndTables()
    {
        for (int i = 0; i < AsteroidPoolSize; i++)
        {
            if (i >= newAsteroids)
            {
                _asteroidPool[i] = Instantiate(_asteroidPrefab.gameObject);
                _asteroidPool[i].transform.position = _objectGraveyardPosition;
            }
        }
        for (int i = 0; i < MaxLaserBeams; i++)
        {
            _laserPool[i] = Instantiate(_laserPrefab);

        }
    }
    #endregion

    /// <summary>
    /// When you create an asteroid using Asteroids Editor and then delete it in a hierarhy window
    /// some values stay not deleted.
    /// checkEditor() fixs it
    /// </summary>
    public void checkEditor()
    {

        for (int i = 0; i < newAsteroids; i++)
        {
            if (_asteroidPool[i] == null)
            {

                Debug.Log("Check Asteroid Pool, _asteroidPool[" + i + "] was empty");
                _asteroidPool[i] = Instantiate(_asteroidPrefab.gameObject);
                _asteroids[i] = new AsteroidDto()
                {
                    Position = new Vector3(0, i, 0),
                    RotationSpeed = UnityEngine.Random.Range(0, 5f),
                    Direction = new Vector3(UnityEngine.Random.Range(-1, 1f), UnityEngine.Random.Range(-1, 1f), 0),
                    Speed = UnityEngine.Random.Range(0.01f, 0.05f),
                    DestroyedThisFrame = false
                };
            }

        }
    }

    public void HighScore()
    {
        var ship = spaceshipRepository.LoadData();
        highScore = ship.highScore;
        if (points > ship.highScore)
        {
            highScore = points;
        }
        spaceshipRepository.SaveData(_spaceshipname.text, highScore);
        _highScore.text = "High Score : " + highScore;
    }

    void OnApplicationQuit()
    {
        HighScore();
    }

    public void NameEntered()
    {
        nameEntered = true;
    }

    float FastSqrt(float number)
    {
        if (number == 0)
            return 0;

        FloatIntUnion u;
        u.tmp = 0;
        u.f = number;
        u.tmp -= 1 << 23; /* Subtract 2^m. */
        u.tmp >>= 1; /* Divide by 2. */
        u.tmp += 1 << 29; /* Add ((b + 1) / 2) * 2^m. */
        return u.f;
    }
}
