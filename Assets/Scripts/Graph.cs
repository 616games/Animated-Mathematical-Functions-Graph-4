using System.Collections.Generic;
using UnityEngine;

public class Graph : MonoBehaviour
{
    #region --Fields / Properties
    
    /// <summary>
    /// Prefab of the point game object used to populate the graph.
    /// </summary>
    [SerializeField]
    private GameObject _pointPrefab;

    /// <summary>
    /// How many points will be plotted.
    /// </summary>
    [SerializeField, Range(10, 100)]
    private int _resolution;

    /// <summary>
    /// The type of graph to be created.
    /// </summary>
    [SerializeField]
    private GraphFunctionNormal _graphFunctionNormal;

    /// <summary>
    /// Toggle only animated graphs.
    /// </summary>
    [SerializeField]
    private bool _animatedGraphsOnly;

    /// <summary>
    /// Toggle displaying a random graph.
    /// </summary>
    [SerializeField]
    private bool _randomGraph;

    /// <summary>
    /// Amount of time to wait before changing to a different function.
    /// </summary>
    [SerializeField]
    private float _changeFunctionTime;

    /// <summary>
    /// Amount of time for the transition between graphs.
    /// </summary>
    [SerializeField]
    private float _transitionDuration = 1f;

    /// <summary>
    /// Tracks elapsed time between function changes and transitions.
    /// </summary>
    private float _durationTimer;
    
    /// <summary>
    /// A List of all the points instantiated to create the graph.
    /// </summary>
    private readonly List<Transform> _points = new List<Transform>();

    /// <summary>
    /// Represents the following equation:  (desired domain range) / (desired resolution)
    /// </summary>
    private float _scaleMultiplier;

    /// <summary>
    /// The scale value to be applied to all points.
    /// </summary>
    private Vector3 _scale;

    /// <summary>
    /// Tracks whether the graph is currently transitioning.
    /// </summary>
    private bool _isMorphing;

    /// <summary>
    /// Function the graph will be transitioning from.
    /// </summary>
    private GraphFunction _transitionFrom;

    /// <summary>
    /// Function the graph will be transitioning to.
    /// </summary>
    private GraphFunction _transitionTo;

    /// <summary>
    /// Input value for "u" and _xInput.
    /// </summary>
    private float _u;

    /// <summary>
    /// Input value for "v" and _zInput.
    /// </summary>
    private float _v;

    #endregion
    
    #region --Unity Specific Methods--
    
    private void Start()
    {
        Init();
        CreateGraphPoints();
    }

    private void Update()
    {
        CheckDuration();
        CheckGraphTransition();
    }
    
    #endregion
    
    #region --Custom Methods--

    /// <summary>
    /// Initializes variables and caches components.
    /// </summary>
    private void Init()
    {
        //Scale is adjusted to make sure all points fit within our domain of -1 to 1 based on how many points are set for the resolution.
        _scaleMultiplier = 2f / _resolution;
        _scale = Vector3.one * _scaleMultiplier;
        _transitionTo = GraphFunctionLibrary.GetGraphFunction(_animatedGraphsOnly, _randomGraph);
    }
    
    /// <summary>
    /// Helper method to prep for the transition between functions.
    /// </summary>
    private void SwitchGraph()
    {
        _isMorphing = true;
        _transitionFrom = _transitionTo;
        _transitionTo = GraphFunctionLibrary.GetGraphFunction(_animatedGraphsOnly, _randomGraph);
    }

    /// <summary>
    /// Tracks time elapsed and transition/change function times.
    /// </summary>
    private void CheckDuration()
    {
        _durationTimer += Time.deltaTime;
        if (_isMorphing)
        {
            if (_durationTimer > _transitionDuration)
            {
                _isMorphing = false;
                _durationTimer = 0.0f;
            }
        }
        else if (_durationTimer > _changeFunctionTime)
        {
                SwitchGraph();
                _durationTimer = 0.0f;
        }
    }

    /// <summary>
    /// Determines which graph animation should be playing.
    /// </summary>
    private void CheckGraphTransition()
    {
        if (_isMorphing)
        {
            AnimateTransition();
        }
        else
        {
            AnimateGraph();
        }
    }

    /// <summary>
    /// Creates a 3D grid of Point game objects.
    /// </summary>
    private void CreateGraphPoints()
    {
        for (int i = 0; i < _resolution * _resolution; i++)
        {
            GameObject _point = Instantiate(_pointPrefab, transform, true);
            _points.Add(_point.transform);
            _point.transform.localScale = _scale;
        }
    }

    /// <summary>
    /// Animates the points of the graph when not transitioning between functions.
    /// </summary>
    private void AnimateGraph()
    {
        //Iterate through all points in every row and update their positions based on the values of "u" and "v" passed into the desired GraphFunctionType.
        //x represents a row of points positioned along the X axis and we need to reset its value at the end of each row (once x reaches the number of points (resolution)).
        //z represents a row of points positioned along the Z axis and we need to increase its value only when x starts to update a new row.
        //Position each point for each row along the X axis from the left shifted right .5 units (radius) so they aren't overlapping.
        //Factor in the point's adjusted scale.
        //Adjust the entire range to the left 1 unit so all points fit within -1 to 1 on the X axis.
        //x and z share the first point's offset and scale values, which is why we set "v"'s initial offset value such that the value of z is 0.
        _v = .5f * _scaleMultiplier - 1f;

        float _time = Time.time;
        for (int _xAxisRowID = 0, _xAxisPointID = 0, _zAxisRowID = 0; _xAxisRowID < _resolution * _resolution; _xAxisRowID++, _xAxisPointID++)
        {
            if (_xAxisPointID == _resolution)
            {
                _xAxisPointID = 0;
                _zAxisRowID++;
                
                //We only need to update the first point in a new row on the z axis once all the points in the row along the x axis have been updated.
                _v = (_zAxisRowID + 0.5f) * _scaleMultiplier - 1f;
            }
            
            _u = (_xAxisPointID + .5f) * _scaleMultiplier - 1f;
            _points[_xAxisRowID].localPosition = _transitionTo(_u, _v, _time);
        }
    }

    /// <summary>
    /// Used specifically for when the graph is transitioning between functions.
    /// </summary>
    private void AnimateTransition()
    {
        _v = .5f * _scaleMultiplier - 1f;
        float _progress = _durationTimer / _transitionDuration;
        float _time = Time.time;
        for (int _xAxisRowID = 0, _xAxisPointID = 0, _zAxisRowID = 0; _xAxisRowID < _resolution * _resolution; _xAxisRowID++, _xAxisPointID++)
        {
            if (_xAxisPointID == _resolution)
            {
                _xAxisPointID = 0;
                _zAxisRowID++;
                _v = (_zAxisRowID + 0.5f) * _scaleMultiplier - 1f;
            }
            
            _u = (_xAxisPointID + .5f) * _scaleMultiplier - 1f;
            _points[_xAxisRowID].position = GraphFunctionLibrary.Morph(_u, _v, _time, _transitionFrom, _transitionTo, _progress);
        }
    }

    #endregion
    
}
