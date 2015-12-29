using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using UnityEngine;

namespace KerbalRogueAI
{
    public class AIXmlParser
    {

        AICore aicore;
        public AIXmlParser(AICore aicore)
        {
            this.aicore = aicore;
        }

        public XmlDocument xmldocument = null;

        public bool LoadDocument(string filename,out XmlNode root)
        {
            xmldocument = new XmlDocument();
            root = null;
            try
            {
                xmldocument.Load(filename);
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.Log("AI Maneuver File not found " + filename);
                return false;
            }
            if (!validateXML(xmldocument))
            {
                Debug.Log("AI Maneuver file failed validate "+filename);
                return false;
            }
            root = xmldocument.FirstChild;
            return true;
            
        }
        private AICondition ParseCondition(XmlNode xmlcondition,AICondition parent=null)
        {
            Console.WriteLine(xmlcondition.Name);
            switch (xmlcondition.Name.ToLower())
            {
                case "landedat":
                    var conditionlanded = new ConditionLanded(aicore);
                    if (xmlcondition.InnerText != "")
                        conditionlanded.LandedAt = xmlcondition.InnerText;
                    return conditionlanded;

                case "enginetype":
                    var conditionengine = new ConditionEngine(aicore);
                    if (xmlcondition.InnerText != "")
                        conditionengine.EngineType = xmlcondition.InnerText;
                    return conditionengine;

                case "encounter":
                    var conditionencounter = new ConditionEncounter(aicore);
                    if (xmlcondition.InnerText != "")
                        conditionencounter.BodyName = xmlcondition.InnerText;
                    return conditionencounter;

                case "deltav":
                    var conditiondeltav = new ConditionDeltaV(aicore);
                    if (xmlcondition.InnerText != "")
                        conditiondeltav.Quantity = Double.Parse(xmlcondition.InnerText);
                    return conditiondeltav;

                case "bodyexists":
                    var conditionbody = new ConditionBody(aicore);
                    if (xmlcondition.InnerText != "")
                        conditionbody.BodyName = xmlcondition.InnerText;
                    return conditionbody;

                case "orbitcircular":
                    var conditioncircular = new ConditionOrbit(aicore);
                    if (xmlcondition.InnerText != "")
                        conditioncircular.Body = xmlcondition.InnerText;
                    return conditioncircular;

                case "massgreater":
                    var conditionmassgt = new ConditionMass(aicore);
                    if (xmlcondition.InnerText != "")
                        conditionmassgt.MassGT = float.Parse(xmlcondition.InnerText);
                    return conditionmassgt;

                case "massless":
                    var conditionmasslt = new ConditionMass(aicore);
                    if (xmlcondition.InnerText != "")
                        conditionmasslt.MassLT = float.Parse(xmlcondition.InnerText);
                    return conditionmasslt;

                case "randomchance":
                    var conditionrandom = new ConditionRandom(aicore);
                    if (xmlcondition.InnerText != "")
                        conditionrandom.Chance = int.Parse(xmlcondition.InnerText);
                    return conditionrandom;

                case "moduletype":
                    var conditionmoduletype = new ConditionModuleType(aicore);
                    if (xmlcondition.InnerText != "")
                        conditionmoduletype.ModuleType = xmlcondition.InnerText;
                    return conditionmoduletype;

                case "vesseltarget":
                    var conditionvesseltarget = new ConditionVesselTarget(aicore);
                    return conditionvesseltarget;

                case "transferwindow":
                    var conditionwindow = new ConditionTransferWindow(aicore);
                    if (xmlcondition.InnerText != "")
                    {
                        CelestialBody body = aicore.GetBody(xmlcondition.InnerText);
                        conditionwindow.destination = body.orbit;
                    }
                    return conditionwindow;

                case "or":
                    var conditionor = new ConditionOr(aicore);
                    foreach (XmlNode child in xmlcondition.ChildNodes)
                    {
                        conditionor.ConditionList.Add(ParseCondition(child));
                    }
                    return conditionor;
                case "and":
                    var conditionand = new ConditionAnd(aicore);
                    foreach (XmlNode child in xmlcondition.ChildNodes)
                    {
                        conditionand.ConditionList.Add(ParseCondition(child));
                    }
                    return conditionand;


                default:
                    return null;
            }
        }

        private AIOperation ParseManeuver(XmlNode xmlmaneuver)
        {
            switch (xmlmaneuver.Name.ToLower())
            {
                case "spaceplanetakeoff":
                    var opspaceplane = new AIOperationSpaceplaneTakeoff(aicore);
                    return opspaceplane;

                case "circularize":
                    var opcircularize = new AIOperationCircularize(aicore);
                    if (xmlmaneuver.InnerText != "")
                        opcircularize.destination = xmlmaneuver.InnerText;
                    return opcircularize;

                case "hohmanntransfer":
                    var optransfer = new AIOperationHohmann(aicore);
                    if (xmlmaneuver.InnerText != "")
                        optransfer.body = xmlmaneuver.InnerText;
                    return optransfer;

                case "coursecorrection":
                    var opadjust = new AIOperationCourseAdjust(aicore);
                    opadjust.distance = Double.Parse(xmlmaneuver.Attributes.GetNamedItem("distance").InnerText);
                    opadjust.body = xmlmaneuver.Attributes.GetNamedItem("target").InnerText;
                    return opadjust;

                case "warpto":
                    var opwarp = new AIOperationWarp(aicore);
                    if (xmlmaneuver.InnerText != "")
                        opwarp.strTo = xmlmaneuver.InnerText;
                    return opwarp;

                case "changepe":
                    var oppe = new AIOperationPE(aicore);
                    if (xmlmaneuver.InnerText != "")
                        oppe.distance = Double.Parse(xmlmaneuver.InnerText);
                    return oppe;

                case "changeap":
                    var opap = new AIOperationAP(aicore);
                    if (xmlmaneuver.InnerText != "")
                        opap.distance = Double.Parse(xmlmaneuver.InnerText);
                    return opap;

                case "rendezvous":
                    var opren = new AIOperationRendezvous(aicore);
                    if (xmlmaneuver.InnerText != "")
                        opren.distance = Double.Parse(xmlmaneuver.InnerText);
                    return opren;

                case "dock":
                    var opdock = new AIOperationDock(aicore);
                    if (xmlmaneuver.Attributes.GetNamedItem("FromGrabber") != null)
                        opdock.FromGrabber = true;
                    else if (xmlmaneuver.Attributes.GetNamedItem("FromDockingPort") != null)
                        opdock.FromDockingPort = true;
                    return opdock;

                case "undock":
                    var opundock = new AIOperationUndock(aicore);
                    return opundock;

                case "planettransfer":
                    string target = xmlmaneuver.InnerText;
                    var planettransfer = new AIOperationPlanetTransfer(aicore, target);
                    return planettransfer;

                default:
                    return null;
            }
        }

        public List<AICondition> GetConditions(string filename)
        {
            List<AICondition> list = new List<AICondition>();
            XmlNode root;
            if (LoadDocument(filename, out root))
            {
                XmlNode child;
                for (int i = 0; i < root.ChildNodes.Count; i++)
                {
                    child = root.ChildNodes[i];
                    if (child.Name.ToLower() == "startconditions")
                    {
                        for (int j = 0; j < child.ChildNodes.Count; j++)
                        {
                            list.Add(ParseCondition(child.ChildNodes[j]));
                        }
                    }
                }
            }
            else
            {
                Debug.Log("AI Invalid file " + filename);
                return null;
            }
            return list;

        }

        public List<AIOperation> GetManeuvers(string filename)
        {
            List<AIOperation> maneuvers = new List<AIOperation>();
            XmlNode root;
            if (LoadDocument(filename, out root))
            {
                XmlNode child;
                for (int i = 0; i < root.ChildNodes.Count; i++)
                {
                    child = root.ChildNodes[i];
                    if (child.Name.ToLower() == "maneuvers")
                    {
                        for (int j = 0; j < child.ChildNodes.Count; j++)
                        {
                            maneuvers.Add(ParseManeuver(child.ChildNodes[j]));
                        }
                    }
                }
            }
            return maneuvers;
        }

        private XmlSchema getSchema()
        {
            XmlSchemaSet xs = new XmlSchemaSet();
            XmlSchema schema;
            XmlReader schemareader = XmlReader.Create("GameData\\RogueAI\\FlightPlans\\Validation.xsd");
            schema = xs.Add(null, schemareader);
            return schema;
        }

        public bool validateXML(XmlDocument doc)
        {
            if (doc.Schemas.Count == 0)
            {
                // Helper method to retrieve schema.
                XmlSchema schema = getSchema();
                doc.Schemas.Add(schema);
            }

            // Use an event handler to validate the XML node against the schema.
            try
            {
                doc.Validate(settings_ValidationEventHandler);
            }
            catch (XmlSchemaValidationException ex)
            {
                Debug.LogError("Message ---" + ex.Message);
                Debug.LogError("HelpLink ---" + ex.HelpLink);
                Debug.LogError("Source ---" + ex.Source);
                Debug.LogError("StackTrace ---" + ex.StackTrace);
                Debug.LogError("TargetSite ---" + ex.TargetSite);
                return false;
            }
            return true;

        }
        void settings_ValidationEventHandler(object sender,
    System.Xml.Schema.ValidationEventArgs e)
        {
            if (e.Severity == XmlSeverityType.Warning)
            {
                Debug.LogError
                    ("The following validation warning occurred: " + e.Message);
                throw new XmlSchemaValidationException("AI Schema Validation failed");
            }
            else if (e.Severity == XmlSeverityType.Error)
            {
                Debug.LogError
                    ("The following critical validation errors occurred: " + e.Message);
                Type objectType = sender.GetType();
                throw new XmlSchemaValidationException("AI Schema Validation failed");
            }

        }
    }

}
