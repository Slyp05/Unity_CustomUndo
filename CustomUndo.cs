using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

#if !UNITY_2022_2_OR_NEWER
using System.Text.RegularExpressions;
#endif

//// a mettre dans editor + add namespace
//// use pour undo et do les DataManager changes
//// upload sur GitHub
static class CustomUndo
{
#if !UNITY_2022_2_OR_NEWER
    //language=regex
    const string UndoGroupNamePattern = @".* \[CUu (\d+)\]";
    //language=regex
    const string RedoGroupNamePattern = @".* \[CUr (\d+)\]";

    static string CreateUndoGroupName(string name, int id) => $"{name} [CUu {id}]";
    static string CreateRedoGroupName(string name, int id) => $"{name} [CUr {id}]";
#endif

    class UndoableAction
    {
        readonly object data;
        readonly Action<object> redoAction;
        readonly Action<object> undoAction;

        bool isDone = false;

        public UndoableAction(object data, Action<object> doAction, Action<object> undoAction) => (this.data, this.redoAction, this.undoAction) = (data, doAction, undoAction);

        public bool Redo()
        {
            if (isDone)
                return false;

            redoAction?.Invoke(data);
            isDone = true;

            return true;
        }

        public bool Undo()
        {
            if (isDone == false)
                return false;

            undoAction?.Invoke(data);
            isDone = false;

            return true;
        }
    }

    static readonly ScriptableObject DummySO;

    static readonly Dictionary<int, UndoableAction> UndoableActions = new Dictionary<int, UndoableAction>();

    static CustomUndo()
    {
        DummySO = ScriptableObject.CreateInstance<ScriptableObject>();
        AssemblyReloadEvents.beforeAssemblyReload += () => Object.DestroyImmediate(DummySO);

#if UNITY_2022_2_OR_NEWER
        Undo.undoRedoEvent += OnUndoRedoEvent;
#else
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
#endif
    }

    public static void DoUndoable(string name, object data, Action<object> doAction, Action<object> undoAction)
    {
        UndoableAction undoableAction = new UndoableAction(data, doAction, undoAction);

#if UNITY_2022_2_OR_NEWER
        Undo.IncrementCurrentGroup();
        Undo.RecordObject(DummySO, name);
        DummySO.name = (string.IsNullOrEmpty(DummySO.name) ? " " : string.Empty);

        UndoableActions[Undo.GetCurrentGroup()] = undoableAction;
#else
        int id = Undo.GetCurrentGroup();
        UndoableActions[id] = undoableAction;

        Undo.IncrementCurrentGroup();
        Undo.RecordObject(DummySO, CreateUndoGroupName(name, id));
        DummySO.name = "undo";

        Undo.IncrementCurrentGroup();
        Undo.RecordObject(DummySO, CreateRedoGroupName(name, id));
        DummySO.name = "redo";
#endif

        undoableAction.Redo();
    }

#if UNITY_2022_2_OR_NEWER
    static void OnUndoRedoEvent(in UndoRedoInfo info)
    {
        if (UndoableActions.TryGetValue(info.undoGroup, out UndoableAction value))
        {
            if (info.isRedo)
                value.Redo();
            else
                value.Undo();
        }
    }
#else
    static void OnUndoRedoPerformed()
    {
        string currGroupName = Undo.GetCurrentGroupName();

        DoUndo(currGroupName);
        DoRedo(currGroupName);
    }

    static void DoUndo(string currGroupName)
    {
        Match match = Regex.Match(currGroupName, UndoGroupNamePattern);
        if (match.Success && UndoableActions.TryGetValue(int.Parse(match.Groups[1].Value), out UndoableAction value))
        {
            if (value.Undo())
            {
                EditorApplication.delayCall += () =>
                {
                    Undo.undoRedoPerformed -= OnUndoRedoPerformed;
                    Undo.PerformUndo();
                    Undo.undoRedoPerformed += OnUndoRedoPerformed;
                };
            }
            else
                EditorApplication.delayCall += Undo.PerformRedo;
        }
    }

    static void DoRedo(string currGroupName)
    {
        Match match = Regex.Match(currGroupName, RedoGroupNamePattern);
        if (match.Success && UndoableActions.TryGetValue(int.Parse(match.Groups[1].Value), out UndoableAction value))
        {
            if (value.Redo())
            {
                EditorApplication.delayCall += () =>
                {
                    Undo.undoRedoPerformed -= OnUndoRedoPerformed;
                    Undo.PerformRedo();
                    Undo.undoRedoPerformed += OnUndoRedoPerformed;
                };
            }
            else
                EditorApplication.delayCall += Undo.PerformUndo;
        }
    }
#endif
}