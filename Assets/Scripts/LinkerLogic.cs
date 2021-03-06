﻿using System.Collections.Generic;
using UnityEngine;

public class LinkerLogic {
    public readonly struct SRemoveRange {
        public int _StartIndex { get; }
        public int _Count { get; }
        public SRemoveRange(int startIndex, int count) {
            _StartIndex = startIndex;
            _Count = count;
        }
    }

    private readonly int _MinimumLinks = 3;
    private FallLogic _FallLogic;
    private List<LinkerObject> _LinkedObjects = new List<LinkerObject>();
    private List<LinkerLine> _LinkedLines = new List<LinkerLine>();

    public LinkerLogic(
        Vector3 boardPosition,
        SGridCoords boardSize,
        BoardTile[,] boardTiles,
        float fallSpeed) {
        _FallLogic = new FallLogic(
            boardPosition,
            boardSize,
            boardTiles,
            fallSpeed
        );
    }

    public void SetSpawners(Dictionary<int, LinkerSpawner> linkerSpawners) {
        _FallLogic.SetSpawners(linkerSpawners);
    }

    public void Initialize() {
        _FallLogic.Initialize();
    }

    public void Update() {
        _FallLogic.Update();
    }

    public bool HasActiveLink() {
        return _LinkedObjects.Count > 0;
    }

    public bool AddLinker(LinkerObject linkerObject) {
        if (_FallLogic.IsCollapsingCollumns()) {
            // ignore inputs if board hasn't settled yet
            return false;
        } else if (linkerObject == GetFocusedObject()) {
            // if compares to self, do nothing
            return false;
        } else if (_LinkedObjects.Contains(linkerObject)) {
            // if already exists, unlink it and all that comes after it
            Unlink(linkerObject);
            return false;
        } else if (!HasActiveLink()
            || (!_LinkedObjects.Contains(linkerObject)
            && linkerObject.gameObject.CompareTag(_LinkedObjects[0].gameObject.tag)
            && IsAdjacent(_LinkedObjects[_LinkedObjects.Count - 1], linkerObject))) {
            // if new AND same type AND adjacent, successful link
            if (_LinkedObjects.Count > 0) {
                _LinkedObjects[_LinkedObjects.Count - 1].SetLinked();
                LinkerLine linkerLine = ObjectPooler.Instance.SpawnFromPool(ObjectPoolTypes.LinkerLine).GetComponent<LinkerLine>();
                linkerLine.CenterPosition(_LinkedObjects[_LinkedObjects.Count - 1].transform.position, linkerObject.transform.position);
                _LinkedLines.Add(linkerLine);
            }
            _LinkedObjects.Add(linkerObject);
            return true;
        }
        return false;
    }

    public void ConfirmLink() {
        if (_FallLogic.IsCollapsingCollumns()) {
            return;
        }
        if (_LinkedObjects.Count < _MinimumLinks) {
            foreach (LinkerObject obj in _LinkedObjects) {
                obj.CancelLink();
            }
        } else {
            List<Vector3> positions = new List<Vector3>();
            foreach (LinkerObject obj in _LinkedObjects) {
                positions.Add(obj.transform.position);
                obj.ConfirmLink();
            }
            ScoreConfig.Instance.AddMovesLeft(-1);
            ScoreConfig.Instance.AddScore(positions);
            _FallLogic.CollapseCollumns();
        }
        foreach (LinkerLine line in _LinkedLines) {
            line.gameObject.SetActive(false);
        }
        _LinkedLines.Clear();
        _LinkedObjects.Clear();
    }

    private LinkerObject GetFocusedObject() {
        if (_LinkedObjects.Count > 0) {
            return _LinkedObjects[_LinkedObjects.Count - 1];
        }
        return null;
    }

    private bool IsAdjacent(LinkerObject fromObj, LinkerObject toObj) {
        if (!fromObj || !toObj) {
            return false;
        }
        return fromObj._GridCoords.IsAdjacent(toObj._GridCoords);
    }

    private void Unlink(LinkerObject linkerObject) {
        if (linkerObject) {
            linkerObject.CancelLink();
            SRemoveRange removeRange = GetRangeFromNextToEnd(linkerObject);
            for (int index = 0; index < _LinkedObjects.Count; ++index) {
                if (index == removeRange._StartIndex - 1) {
                    _LinkedObjects[index].SetFocused();
                } else if (index >= removeRange._StartIndex) {
                    _LinkedObjects[index].CancelLink();
                }
            }
            if (removeRange._Count > 0) {
                _LinkedObjects.RemoveRange(removeRange._StartIndex, removeRange._Count);
            }
            for (int lineIndex = 0; lineIndex < _LinkedLines.Count; ++lineIndex) {
                if (lineIndex >= removeRange._StartIndex - 1) {
                    _LinkedLines[lineIndex].gameObject.SetActive(false);
                }
            }
            _LinkedLines.RemoveRange(_LinkedObjects.Count - 1, removeRange._Count);
        }
    }

    private SRemoveRange GetRangeFromNextToEnd(LinkerObject linkerObject) {
        for (int index = 0; index < _LinkedObjects.Count; ++index) {
            if (_LinkedObjects[index] == linkerObject) {
                if (index < _LinkedObjects.Count - 1) {
                    return new SRemoveRange(index + 1, _LinkedObjects.Count - (index + 1));
                }
            }
        }
        return new SRemoveRange(0, 0);
    }
}
