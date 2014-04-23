Shader "Custom/VertexColor" {
Properties {
	_EmisColor ("Color", Color) = (.2,.2,.2,0)
	_MainTex ("Base (RGB)", 2D) = "white" {}
}

SubShader {
	Tags { "RenderType"="Opaque" }
	Tags { "LightMode" = "Vertex" }
	Lighting On
	//Material { Emission [_EmisColor] }
	ColorMaterial AmbientAndDiffuse
	//ZWrite Off
	ColorMask RGB
	//Blend SrcAlpha OneMinusSrcAlpha
	//AlphaTest Greater .001
	Pass { 
		SetTexture [_MainTex] { combine primary * texture }
	}
}
}
