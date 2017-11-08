using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

namespace HoloRecord
{
    public class StartRecord : MonoBehaviour, IInputClickHandler
    {
        public RecordController RecordController;

        public void OnInputClicked(InputClickedEventData eventData)
        {
            RecordController.StartRecord();
        }

    } // class
} // namespace
