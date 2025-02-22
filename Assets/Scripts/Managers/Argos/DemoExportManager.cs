﻿using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml;
using Managers.Argos.XML;
using UnityEngine;

namespace Managers.Argos
{
    // This class is responsible for exporting demo data to an Argos compatible XML file
    public class DemoExportManager : MonoBehaviour
    {
        // Reference to the ArenaObjectsManager which manages the objects in the scene
        public ArenaObjectsManager arenaObjectsManager;

        public ArgosFileLoader argosFileLoader;

        // Path to the current XML file
        public string currentXML;

        [DllImport("__Internal")]
        private static extern void registerMessageCallback();

        private void Start()
        {
#if UNITY_WEBGL
            registerMessageCallback();
#endif

            // If the Argos file path is set, set the current XML path to it
            if (GlobalVariables.HasKey(GlobalVariablesKey.ArgosFile))
            {
                currentXML = GlobalVariables.Get<string>(GlobalVariablesKey.ArgosFile);
            }
        }

        private void Update()
        {
            // Check if Left Control key and E key are pressed simultaneously
            if (!Input.GetKeyDown(KeyCode.E) || !Input.GetKey(KeyCode.LeftControl))
            {
                return;
            }

            // Call the UpdateAsync method asynchronously
#pragma warning disable CS4014
            UpdateDemoData();
#pragma warning restore CS4014
        }

        // This method is called when the "TriggerSave" event is triggered
        public void OnTriggerSave()
        {
            UpdateDemoData();
        }

        // This method updates the demo data and saves it to the XML file
        private void UpdateDemoData()
        {
            var doc = new XmlDocument();
            doc.LoadXml(argosFileLoader.GetArgosFileLoader().GetContentFromPathOrUrl(currentXML));

            var loopFunctions = (XmlElement)doc.GetElementsByTagName("loop_functions")[0];

            // Get the last demo ID and increment it
            var greatestDemoId = 0;

            foreach (var oldDemo in loopFunctions.GetElementsByTagName("demo"))
            {
                var idString = ((XmlElement)oldDemo).GetAttribute("id");

                // Remove any non-numeric characters from the ID string
                idString = Regex.Replace(idString, "[^0-9]", "");

                // If the ID string can be parsed as an integer, compare it to the current greatest ID
                if (!int.TryParse(idString, out var demoId)) continue;

                if (demoId > greatestDemoId)
                {
                    greatestDemoId = demoId;
                }
            }

            // Create a new demo element with the incremented ID
            var demo = doc.CreateElement("demo");
            demo.SetAttribute("id", "demo_" + (greatestDemoId + 1));

            var currentIndex = 0;

            // For each robot in the scene, create an "epuck" element with its ID and position
            foreach (var arenaGameObject in arenaObjectsManager.GetObjects())
            {
                var argosTagForObject = arenaGameObject.GetComponent<ArgosInfo>().tag;

                // If the object is not a robot, skip to the next one
                if (argosTagForObject != ArgosTag.Robot)
                {
                    continue;
                }

                var robot = doc.CreateElement("epuck");
                robot.SetAttribute("id", "Epuck-" + currentIndex);
                robot.SetAttribute("position",
                    ArgosHelper.VectorToArgosVectorNoHeight2D(arenaGameObject.transform.position));
                demo.AppendChild(robot);
                currentIndex += 1;
            }
            
            // If there are no robots in the scene, skip the rest of the method, no need to save the demo
            // since it would hold no useful information.
            if (currentIndex == 0)
            {
                return;
            }

            // Append the new demo element to the loop functions element
            loopFunctions.AppendChild(demo);


            // Save the modified XML document to the current XML file
            argosFileLoader.SaveXmlFile(currentXML, doc);

            arenaObjectsManager.Reset();
        }
    }
}   