//#define UNI_18892
//#define UNI_18844
//#define UNI_18848
//#define UNI_18850
// ***********************************************************************
// Copyright (c) 2017 Unity Technologies. All rights reserved.  
//
// Licensed under the ##LICENSENAME##. 
// See LICENSE.md file in the project root for full license information.
// ***********************************************************************

using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FbxSdk;

namespace FbxSdk.Examples
{
    namespace Editor
    {

        public class FbxImporter04 : System.IDisposable
        {
            const string Title =
                 "Example 04: import scene static mesh with UVs";

            const string Subject =
                 @"Example FbxImporter04 illustrates how to:
                                1) create and initialize the importer
                                2) import the scene
                                3) visit each node in hierarchy
                                4) construct local translation, rotation and scaling taking pre/post rotations, pivots and offsets into effect
                                5) create mesh filter and renderer
                                6) assign mesh attributes: uv, uv2, uv3, uv4
                                7) create a game object with same name, parent, transformation matrix, mesh and, mesh attributes
                    ";

            const string Keywords =
                 "import scene node hierarchy static mesh uv normal tangent vertexcolor";

            const string Comments =
                 @"";

            const string MenuItemName = "File/Import FBX/04. Import UVs";

            /// <summary>
            /// Create instance of example
            /// </summary>
            public static FbxImporter04 Create () { return new FbxImporter04 (); }

            private Vector2 [] ProcessUVSet (FbxLayerElementUV element,
                                            int [] indices,
                                            int indexCount,
                                            int vertexCount)
            {
                Vector2 [] result = new Vector2 [indexCount];

                FbxLayerElement.EReferenceMode referenceMode = element.GetReferenceMode ();
                FbxLayerElement.EMappingMode mappingMode = element.GetMappingMode ();

                // direct or via-index
                bool isDirect = referenceMode == FbxLayerElement.EReferenceMode.eDirect;

                FbxLayerElementArray fbxElementArray = element.GetDirectArray ();
                FbxLayerElementArray fbxIndexArray = isDirect ? null : element.GetIndexArray ();

                if (mappingMode == FbxLayerElement.EMappingMode.eByControlPoint) {

                    if (fbxElementArray.GetCount () != vertexCount) {
                        return null;
                    }

                    for (int i = 0; i < indexCount; i++) {
                        int index = i;
                        if (!isDirect) {
#if UNI_18850
                            fbxIndexArray.GetAt (i, out index);
#endif
                        }

#if UNI_18850
                        fbxElementArray.GetAt (indices [index], out result [i]);
#endif
                        // UVs in FBX can contain NaNs, so we set these vertices to (0,0)
                        if (float.IsNaN (result [i] [0]) || float.IsNaN (result [i] [1])) {
                            Debug.LogWarning (string.Format ("invalid UV detected at {0}", i));
                            result [i] = Vector2.zero;
                        }
                    }

                } else if (mappingMode == FbxLayerElement.EMappingMode.eAllSame) {
                    Vector2 value = new Vector2 ();

#if UNI_18850
                    if (fbxElementArray.GetAt (0, value)) 
#endif
                    {
                        for (int i = 0; i < indexCount; i++) {
                            result [i] = value;
                        }
                    }
                } else {
                    Debug.LogError ("unsupported UV-to-Component mapping mode");
                }

                return result;
            }

            private void ProcessUVs (FbxMesh fbxMesh, Mesh unityMesh, int maxUVs = 4)
            {
                // Import UV sets (maximum defined by maxUVs)
                int uvsetIndex = 0;

                // First just try importing diffuse UVs from separate layers
                // (Maya exports that way)
                FbxLayerElementUV fbxFirstUVSet = null;
                FbxLayer fbxFirstUVLayer = null;

#if UNI_18850
                int [] polygonVertexIndices = fbxMesh.GetPolygonVertices ();
                // NOTE: assuming triangles
                int polygonIndexCount = fbxMesh.GetPolygonCount () * 3;
                int vertexCount = fbxMesh.GetControlPointsCount ();
                for (int i = 0; i < fbxMesh.GetLayerCount(); i++)
                {
                    FbxLayer fbxLayer = fbxMesh.GetLayer (i);
                    if (fbxLayer==null)
                        continue;
                    
                    FbxLayerElementUV fbxUVSet = fbxLayer.GetUVs ();
                    if (fbxUVSet==null)
                        continue;

                    if (fbxFirstUVSet!=null)
                    {
                        fbxFirstUVSet = fbxUVSet;
                        fbxFirstUVLayer = fbxLayer;
                    }

                    switch (uvsetIndex) 
                    {
                        case 0: 
                        unityMesh.uv = ProcessUVSet (fbxUVSet, polygonVertexIndices, polygonIndexCount, vertexCount);
                        break;
                        case 1: 
                        unityMesh.uv2 = ProcessUVSet (fbxUVSet, polygonVertexIndices, polygonIndexCount, vertexCount);
                        break;
                        case 2: 
                        unityMesh.uv3 = ProcessUVSet (fbxUVSet, polygonVertexIndices, polygonIndexCount, vertexCount);
                        break;
                        case 3: 
                        unityMesh.uv4 = ProcessUVSet (fbxUVSet, polygonVertexIndices, polygonIndexCount, vertexCount);
                        break;

                    }
                    uvsetIndex++;

                    if (uvsetIndex == maxUVs)
                        break;
                    }
                    // If we have received one UV set, check whether the same layer contains an emissive UV set
                    // that is different from diffuse UV set.
                    // 3dsmax FBX exporters doesn't export UV sets as different layers, instead for lightmapping usually
                    // a material is set up to have lightmap (2nd UV set) as self-illumination slot, and main texture
                    // (1st UV set) as diffuse slot.
                    if (uvsetIndex == 1 && fbxFirstUVSet!=null)
                    {
                        FbxLayerElementUV fbxSecondaryUVSet = null;

                        // TODO: check if we've already passed eTextureEmissive layer
                        for (int i = FbxLayerElement.EType.eTextureEmissive; i < FbxLayerElement.EType.eTypeCount; i++)
                        {
                            fbxSecondaryUVSet = fbxFirstUVLayer.GetUVs ((FbxLayerElement.EType)i);
                            if (fbxSecondaryUVSet != null)
                                break;
                        }

                        if (fbxSecondaryUVSet!=null)
                        {
                            unityMesh.uv2 = ProcessUVSet (fbxSecondaryUVSet,
                                                          polygonVertexIndices,
                                                          polygonIndexCount,
                                                          vertexCount);
                            uvsetIndex++;
                        }
                    }
                }
#endif
            }
                
            private void ProcessMesh (FbxNode fbxNode, GameObject unityGo)
            {
                FbxMesh fbxMesh = fbxNode.GetMesh ();
                if (fbxMesh == null) return;

                var unityMesh = new Mesh ();

                // create mesh
                var unityVertices = new List<Vector3> ();
                var unityTriangleIndices = new List<int> ();

                // transfer vertices
                for (int i = 0; i < fbxMesh.GetControlPointsCount (); ++i) 
                {
                    FbxVector4 fbxVector4 = fbxMesh.GetControlPointAt (i);

                    Debug.Assert (fbxVector4.X <= float.MaxValue && fbxVector4.X >= float.MinValue);
                    Debug.Assert (fbxVector4.Y <= float.MaxValue && fbxVector4.Y >= float.MinValue);
                    Debug.Assert (fbxVector4.Z <= float.MaxValue && fbxVector4.Z >= float.MinValue);

                    unityVertices.Add (new Vector3 ((float)fbxVector4.X, (float)fbxVector4.Y, (float)fbxVector4.Z));
                }
#if UNI_18848
                // transfer triangles
                for (int polyIndex = 0; polyIndex < fbxMesh.GetPolygonCount (); ++polyIndex )
                {
                    int polySize = fbxMesh.GetPolygonSize (polyIndex);

                    // only support triangles
                    Debug.Assert (polySize == 3);

                    for (int polyVertexIndex = 0; polyVertexIndex < polySize; ++polyVertexIndex)
                    {
                        int vertexIndex = fbxMesh.GetPolygonVertex(polyIndex, polyVertexIndex);

                        unityTriangleIndices.Add (vertexIndex);
                    }
                }
#endif
                unityMesh.vertices = unityVertices.ToArray ();

                // TODO: 
                // - support Mesh.SetTriangles - multiple materials per mesh
                // - support Mesh.SetIndices - other topologies e.g. quads
                unityMesh.triangles = unityTriangleIndices.ToArray ();

                ProcessUVs (fbxMesh, unityMesh);

                var unityMeshFilter = unityGo.AddComponent<MeshFilter> ();
                unityMeshFilter.sharedMesh = unityMesh;

                var unityRenderer = unityGo.AddComponent<MeshRenderer> ();
                {
                    // Assign the default material (hack!)
                    var unityPrimitive = GameObject.CreatePrimitive (PrimitiveType.Quad);
                    var unityMat = unityPrimitive.GetComponent<MeshRenderer> ().sharedMaterial;
                    unityRenderer.sharedMaterial = unityMat;
                    UnityEngine.Object.DestroyImmediate (unityPrimitive);
                }
            }

            private void ProcessTransform (FbxNode fbxNode, GameObject unityGo)
            {
                FbxVector4 lclTrs = new FbxVector4 ();
                FbxQuaternion lclRot = new FbxQuaternion ();
                FbxVector4 lclScl = new FbxVector4 (1.0f, 1.0f, 1.0f);

#if UNI_18844
                // Construct rotation matrices
                FbxVector4 fbxRotation = new FbxVector4 (fbxNode.LclRotation.Get ());
                FbxAMatrix fbxRotationM = new FbxAMatrix ();
                fbxRotationM.SetR(fbxRotation);

                FbxVector4 fbxPreRotation = new FbxVector4 (fbxNode.PreRotation.Get ());
                FbxAMatrix fbxPreRotationM = new FbxAMatrix ();
                fbxPreRotationM.SetR(fbxPreRotation);

                FbxVector4 fbxPostRotation = new FbxVector4 (fbxNode.PostRotation.Get ());
                FbxAMatrix fbxPostRotationM = new FbxAMatrix ();
                fbxPostRotationM.SetR(fbxPostRotation);

                // Construct translation matrix
                FbxAMatrix fbxTranslationM = new FbxAMatrix ();
                FbxVector4 fbxTranslation = new FbxVector4 (fbxNode.LclTranslation.Get ());
                fbxTranslationM.SetT(fbxTranslation);

                // Construct scaling matrix
                FbxAMatrix fbxScalingM = new FbxAMatrix ();
                FbxVector4 fbxScaling = new FbxVector4 (fbxNode.LclScaling.Get ());
                fbxScalingM.SetS(fbxScaling);

                // Construct offset and pivot matrices
                FbxAMatrix fbxRotationOffsetM = new FbxAMatrix ();
                FbxVector4 fbxRotationOffset = fbxNode.RotationOffset.Get ();
                fbxRotationOffsetM.SetT(fbxRotationOffset);

                FbxAMatrix fbxRotationPivotM = new FbxAMatrix ();
                FbxVector4 fbxRotationPivot = fbxNode.RotationPivot.Get ();
                fbxRotationPivotM.SetT(fbxRotationPivot);

                FbxAMatrix fbxScalingOffsetM = new FbxAMatrix ();
                FbxVector4 fbxScalingOffset = fbxNode.ScalingOffset.Get ();
                fbxScalingOffsetM.SetT(fbxScalingOffset);

                FbxAMatrix fbxScalingPivotM = new FbxAMatrix ();
                FbxVector4 fbxScalingPivot = fbxNode.ScalingPivot.Get ();
                fbxScalingPivotM.SetT(fbxScalingPivot);

                FbxAMatrix fbxTransform = 
                    fbxTranslationM * 
                    fbxRotationOffsetM * 
                    fbxRotationPivotM * 
                    fbxPreRotationM * 
                    fbxRotationM * 
                    fbxPostRotationM *
                    fbxRotationPivotM.Inverse () *
                    fbxScalingOffsetM * 
                    fbxScalingPivotM * 
                    fbxScalingM *
                    fbxScalingPivotM.Inverse ();
                
                FbxVector4 lclTrs = fbxTransform.GetT ();
                FbxQuaternion lclRot = fbxTransform.GetQ ();
                FbxVector4 lclScl = fbxTransform.GetS ();
#endif

                Debug.Log (string.Format ("processing {3} Lcl : T({0}) R({1}) S({2})",
                                         lclTrs.ToString (),
                                         lclRot.ToString (),
                                         lclScl.ToString (),
                                         fbxNode.GetName ()));

                unityGo.transform.localPosition = new Vector3 ((float)lclTrs[0], (float)lclTrs[1], (float)lclTrs[2]);
                unityGo.transform.localRotation = new Quaternion ((float)lclRot[0], (float)lclRot[1], (float)lclRot[2], (float)lclRot[3]);
                unityGo.transform.localScale = new Vector3 ((float)lclScl[0], (float)lclScl[1], (float)lclScl[2]);
            }

            /// <summary>
            /// Process fbx scene by doing nothing
            /// </summary>
            public void ProcessNode (FbxNode fbxNode, GameObject unityParentObj = null)
            {
                string name = fbxNode.GetName ();

                GameObject unityGo = new GameObject (name);
                NumNodes++;

                if (unityParentObj != null) {
                    unityGo.transform.parent = unityParentObj.transform;
                }

                ProcessTransform (fbxNode, unityGo);
                ProcessMesh (fbxNode, unityGo);

                for (int i = 0; i < fbxNode.GetChildCount (); ++i) {
                    ProcessNode (fbxNode.GetChild (i), unityGo);
                }
            }

            /// <summary>
            /// Convert scene's system units but leave scaling unchanged
            /// </summary>
            public void ConvertScene (FbxScene fbxScene, FbxSystemUnit toUnits)
            {
                // Get scale factor.
                float scaleFactor = 1.0f;

#if UNI_18844
                scaleFactor = fbxScene.GetGlobalSettings ().GetSystemUnit ().GetConversionFactorTo (toUnits);
#endif

                if (scaleFactor.Equals (1.0f))
                    return;

                // Get root node.
                FbxNode fbxRootNode = fbxScene.GetRootNode ();

                // For all the nodes to convert the translations
                Queue<FbxNode> fbxNodes = new Queue<FbxNode> ();

                fbxNodes.Enqueue (fbxRootNode);

                while (fbxNodes.Count > 0) {
                    FbxNode fbxNode = fbxNodes.Dequeue ();

                    // Convert node's translation.
                    FbxDouble3 lclTrs = fbxNode.LclTranslation.Get ();

#if UNI_18844
                    lclTrs *= scaleFactor;
                    lclTrs *= scaleFactor;
                    lclTrs *= scaleFactor;
#endif
                    fbxNode.LclTranslation.Set (lclTrs);

                    FbxMesh fbxMesh = fbxNode.GetMesh ();

                    if (fbxMesh != null) {
                        for (int i = 0; i < fbxMesh.GetControlPointsCount (); ++i) {
                            FbxVector4 fbxVector4 = fbxMesh.GetControlPointAt (i);
#if UNI_18844
                            fbxVector4 *= scaleFactor;
#endif
                            fbxMesh.SetControlPointAt (fbxVector4, i);
                        }
                    }

                    for (int i = 0; i < fbxNode.GetChildCount (); ++i) {
                        fbxNodes.Enqueue (fbxNode.GetChild (i));
                    }
                }
            }

            /// <summary>
            /// Process fbx scene by doing nothing
            /// </summary>
            public void ProcessScene (FbxScene fbxScene)
            {
                Debug.Log (string.Format ("Scene name: {0}", fbxScene.GetName()));

                var fbxSettings = fbxScene.GetGlobalSettings ();
                FbxSystemUnit fbxSystemUnit = fbxSettings.GetSystemUnit ();

                if (fbxSystemUnit != UnitySystemUnit) 
                {
                    Debug.Log (string.Format ("converting system unit to match Unity. Expected {0}, found {1}",
                                             UnitySystemUnit, fbxSystemUnit));

                    ConvertScene (fbxScene, UnitySystemUnit);
                } 
                else 
                {
                    Debug.Log (string.Format("file system units {0}", fbxSystemUnit));
                }

                // The Unity axis system has Y up, Z forward, X to the right.
                FbxAxisSystem fbxAxisSystem = fbxSettings.GetAxisSystem ();

                if (fbxAxisSystem != UnityAxisSystem) {
                    Debug.LogWarning (string.Format ("file axis system do not match Unity, Expected {0} found {1}",
                                                     AxisSystemToString (UnityAxisSystem),
                                                     AxisSystemToString (fbxAxisSystem)));
                }

                ProcessNode (fbxScene.GetRootNode ());

                return;
            }


            /// <summary>
            /// Import all from scene.
            /// Return the number of objects we imported.
            /// </summary>
            public int ImportAll (IEnumerable<Object> unitySelectionSet)
            {
                // Create the FBX manager
                using (var fbxManager = FbxManager.Create ()) 
                {
                    FbxIOSettings fbxIOSettings = FbxIOSettings.Create (fbxManager, Globals.IOSROOT);

                    // Configure the IO settings.
                    fbxManager.SetIOSettings (fbxIOSettings);

                    // Get the version number of the FBX files generated by the
                    // version of FBX SDK that you are using.
                    int sdkMajor = -1, sdkMinor = -1, sdkRevision = -1;

                    FbxManager.GetFileFormatVersion (out sdkMajor, out sdkMinor, out sdkRevision);

                    // Create the importer 
                    var fbxImporter = FbxImporter.Create (fbxManager, "Importer");

                    // Initialize the importer.
                    int fileFormat = -1;

                    bool status = fbxImporter.Initialize (LastFilePath, fileFormat, fbxIOSettings);
                    FbxStatus fbxStatus = fbxImporter.GetStatus ();

                    // Get the version number of the FBX file format.
                    int fileMajor = -1, fileMinor = -1, fileRevision = -1;

#if UNI_18892
                    fbxImporter.GetFileVersion (fileMajor, fileMinor, fileRevision);
#endif
                    // Check that initialization of the fbxImporter was successful
                    if (!status) 
                    {
                        Debug.LogError (string.Format ("failed to initialize FbxImporter, error returned {0}", 
                                                       fbxStatus.GetErrorString ()));

                        if (fbxStatus.GetCode () == FbxStatus.EStatusCode.eInvalidFileVersion) 
                        {
                            Debug.LogError (string.Format ("Invalid file version detected\nSDK version: {0}.{1}.{2}\nFile version: {3}.{4}.{5}",
                                                           sdkMajor, sdkMinor, sdkRevision, 
                                                           fileMajor, fileMinor, fileRevision));

                        }

                        return 0;
                    }

                    // Import options. Determine what kind of data is to be imported.
                    // The default is true, but here we set the options explictly.
#if UNI_18892
                    fbxIOSettings.SetBoolProp(Globals.IMP_FBX_MATERIAL,         false);
                    fbxIOSettings.SetBoolProp(Globals.IMP_FBX_TEXTURE,          false);
                    fbxIOSettings.SetBoolProp(Globals.IMP_FBX_LINK,             false);
                    fbxIOSettings.SetBoolProp(Globals.IMP_FBX_SHAPE,            false);
                    fbxIOSettings.SetBoolProp(Globals.IMP_FBX_GOBO,             false);
                    fbxIOSettings.SetBoolProp(Globals.IMP_FBX_ANIMATION,        false);
                    fbxIOSettings.SetBoolProp(Globals.IMP_FBX_GLOBAL_SETTINGS,  true);
#endif

                    // Create a scene
                    var fbxScene = FbxScene.Create (fbxManager, "Scene");

                    // Import the scene to the file.
                    status = fbxImporter.Import (fbxScene);

                    if (status == false) 
                    {
                        Debug.LogError (string.Format ("failed to import file ({0})", 
                                                       fbxImporter.GetStatus ().GetErrorString ()));
                    } 
                    else 
                    {
                        // import data into scene 
                        ProcessScene (fbxScene);
                    }

                    // cleanup
                    fbxScene.Destroy ();
                    fbxImporter.Destroy ();

                    return status == true ? NumNodes : 0;
                }
            }

            // 
            // Create a simple user interface (menu items)
            //
            /// <summary>
            /// create menu item in the File menu
            /// </summary>
            [MenuItem (MenuItemName, false)]
            public static void OnMenuItem ()
            {
                OnImport();
            }

            /// <summary>
            // Validate the menu item defined by the function above.
            /// </summary>
            [MenuItem (MenuItemName, true)]
            public static bool OnValidateMenuItem ()
            {
                // Return true
                return true;
            }

            /// <summary>
            /// Number of nodes imported including siblings and decendents
            /// </summary>
            public int NumNodes { private set; get; }

            /// <summary>
            /// Clean up this class on garbage collection
            /// </summary>
            public void Dispose () { }

            public bool Verbose { private set; get; }

            /// <summary>
            /// manage the selection of a filename
            /// </summary>
            static string LastFilePath { get; set; }
            const string kExtension = "fbx";
            const string kBorderLine = "--------------------\n";
            const string kNewLine = "\n";
            const string kPadding = "    ";

            private static string MakeFileName(string basename = "test", string extension = "fbx")
            {
                return basename + "." + extension;
            }

            private FbxSystemUnit UnitySystemUnit { get { return FbxSystemUnit.m; } }

            private FbxAxisSystem UnityAxisSystem { 
                get { return new FbxAxisSystem (FbxAxisSystem.EUpVector.eYAxis, 
                                                FbxAxisSystem.EFrontVector.eParityOdd, 
                                                FbxAxisSystem.ECoordSystem.eLeftHanded); }
            }

            private static string AxisSystemToString(FbxAxisSystem fbxAxisSystem) {
                return string.Format ("[{0}, {1}, {2}]",
                                      fbxAxisSystem.GetUpVector ().ToString (),
                                      fbxAxisSystem.GetFrontVector ().ToString (),
                                      fbxAxisSystem.GetCoorSystem ().ToString ());
            }

            // use the SaveFile panel to allow user to enter a file name
            private static void OnImport()
            {
                // Now that we know we have stuff to import, get the user-desired path.
                var directory = string.IsNullOrEmpty (LastFilePath) 
                                      ? Application.dataPath 
                                      : Path.GetDirectoryName (LastFilePath);
                
                var title = "Import FBX";

                var filePath = EditorUtility.OpenFilePanel(title, directory, kExtension);

                if (string.IsNullOrEmpty (filePath)) 
                {
                    return;
                }

                LastFilePath = filePath;

                using (var fbxImporter = Create()) 
                {
                    if (fbxImporter.ImportAll(Selection.objects) > 0)
                    {
                        Debug.Log (string.Format ("Successfully imported: {0}", filePath));
                    }
                }
            }
        }
    }
}
