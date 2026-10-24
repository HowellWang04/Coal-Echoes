// Apply the current ink palette. Preview.cs preserves later Inspector adjustments.
UnityEditor.AssetDatabase.ImportAsset("Assets/Scripts/MainMenu/NewspaperHalftone.shader");
var material = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Art Resources/UI/NewspaperMenu/Halftone/NewspaperHalftone.mat");
material.SetColor("_PaperColor", new UnityEngine.Color(0.865f, 0.85f, 0.82f, 1));
material.SetColor("_InkColor", new UnityEngine.Color(0.16f, 0.16f, 0.155f, 1));
material.SetFloat("_CircleDensity", 150);
material.SetFloat("_Contrast", 2.2f);
material.SetFloat("_PaperAmount", 0.55f);
material.SetFloat("_DotStrength", 0.12f);
material.SetFloat("_InkDensity", 0.94f);
material.SetFloat("_InkSpread", 1.15f);
material.SetFloat("_InkWear", 0.22f);
UnityEditor.EditorUtility.SetDirty(material);
UnityEditor.AssetDatabase.SaveAssetIfDirty(material);
return UnityEditor.ShaderUtil.GetShaderMessages(material.shader).Select(m => m.message).ToArray();
