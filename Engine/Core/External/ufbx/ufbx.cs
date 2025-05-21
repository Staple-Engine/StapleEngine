using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace ufbx;

#if UFBX_INTERNAL
internal
#else
public
#endif
partial class ufbx
{
    public const uint UFBXNoIndex = ~0u;

    /// <summary>
    /// (Order in which Euler-angle rotation axes are applied for a transform
    /// NOTE: The order in the name refers to the order of axes *applied*,
    /// not the multiplication order: eg. `UFBX_ROTATION_ORDER_XYZ` is `Z*Y*X`
    /// </summary>
    public enum UFBXRotationOrder
    {
        XYZ,
        XZY,
        YZX,
        YXZ,
        ZXY,
        ZYX,
        Spheric
    }

    public enum UFBXDomValueType
    {
        Number,
        String,
        ArrayI8,
        ArrayI16,
        ArrayI64,
        ArrayF32,
        ArrayF64,
        ArrayRawString,
        ArrayIgnored,
    }

    /// <summary>
    /// Data type contained within the property. All the data fields are always
    /// populated regardless of type, so there's no need to switch by type usually
    /// eg. `prop->value_real` and `prop->value_int` have the same value (well, close)
    /// if `prop->type == INTEGER`. String values are not converted from/to.
    /// </summary>
    public enum UFBXPropType
    {
        Unknown,
        Boolean,
        Integer,
        Number,
        Vector,
        Color,
        ColorWithAlpha,
        String,
        DateTime,
        Translation,
        Rotation,
        Scaling,
        Distance,
        Compound,
        Blob,
        Reference,
    }

    /// <summary>
    /// Property flags: Advanced information about properties, not usually needed.
    /// </summary>
    public enum UFBXPropFlags
    {
        /// <summary>
        /// Supports animation.
        /// NOTE: ufbx ignores this and allows animations on non-animatable properties.
        /// </summary>
        Animatable = 0x1,

        /// <summary>
        /// User defined (custom) property.
        /// </summary>
        UserDefined = 0x2,

        /// <summary>
        /// Hidden in UI.
        /// </summary>
        Hidden = 0x4,

        /// <summary>
        /// Disallow modification from UI for components.
        /// </summary>
        LockX = 0x10,
        /// <summary>
        /// Disallow modification from UI for components.
        /// </summary>
        LockY = 0x20,
        /// <summary>
        /// Disallow modification from UI for components.
        /// </summary>
        LockZ = 0x40,
        /// <summary>
        /// Disallow modification from UI for components.
        /// </summary>
        LockW = 0x80,

        /// <summary>
        /// Disable animation from components.
        /// </summary>
        MuteX = 0x100,
        /// <summary>
        /// Disable animation from components.
        /// </summary>
        MuteY = 0x200,
        /// <summary>
        /// Disable animation from components.
        /// </summary>
        MuteZ = 0x400,
        /// <summary>
        /// Disable animation from components.
        /// </summary>
        MuteW = 0x800,

        /// <summary>
        /// Property created by ufbx when an element has a connected `ufbx_anim_prop`
        /// but doesn't contain the `ufbx_prop` it's referring to.
        /// NOTE: The property may have been found in the templated defaults.
        /// </summary>
        Synthetic = 0x1000,

        /// <summary>
        /// The property has at least one `ufbx_anim_prop` in some layer.
        /// </summary>
        Animated = 0x2000,

        /// <summary>
        /// Used by `ufbx_evaluate_prop()` to indicate the the property was not found.
        /// </summary>
        NBotFound = 0x4000,

        /// <summary>
        /// The property is connected to another one.
        /// This use case is relatively rare so `ufbx_prop` does not track connections
        /// directly. You can find connections from `ufbx_element.connections_dst` where
        /// `ufbx_connection.dst_prop` is this property and `ufbx_connection.src_prop` is defined.
        /// </summary>
        Connected = 0x8000,

        /// <summary>
        /// The value of this property is undefined (represented as zero).
        /// </summary>
        NoValue = 0x10000,

        /// <summary>
        /// This property has been overridden by the user.
        /// See `ufbx_anim.prop_overrides` for more information.
        /// </summary>
        Overriden = 0x20000,

        /// <summary>
        /// Value type.
        /// `REAL/VEC2/VEC3/VEC4` are mutually exclusive but may coexist with eg. `STRING`
        /// in some rare cases where the string defines the unit for the vector.
        /// </summary>
        Real = 0x100000,
        /// <summary>
        /// Value type.
        /// `REAL/VEC2/VEC3/VEC4` are mutually exclusive but may coexist with eg. `STRING`
        /// in some rare cases where the string defines the unit for the vector.
        /// </summary>
        Vec2 = 0x200000,
        /// <summary>
        /// Value type.
        /// `REAL/VEC2/VEC3/VEC4` are mutually exclusive but may coexist with eg. `STRING`
        /// in some rare cases where the string defines the unit for the vector.
        /// </summary>
        Vec3 = 0x400000,
        /// <summary>
        /// Value type.
        /// `REAL/VEC2/VEC3/VEC4` are mutually exclusive but may coexist with eg. `STRING`
        /// in some rare cases where the string defines the unit for the vector.
        /// </summary>
        Vec4 = 0x800000,
        /// <summary>
        /// Value type.
        /// `REAL/VEC2/VEC3/VEC4` are mutually exclusive but may coexist with eg. `STRING`
        /// in some rare cases where the string defines the unit for the vector.
        /// </summary>
        Int = 0x1000000,
        /// <summary>
        /// Value type.
        /// `REAL/VEC2/VEC3/VEC4` are mutually exclusive but may coexist with eg. `STRING`
        /// in some rare cases where the string defines the unit for the vector.
        /// </summary>
        String = 0x2000000,
        /// <summary>
        /// Value type.
        /// `REAL/VEC2/VEC3/VEC4` are mutually exclusive but may coexist with eg. `STRING`
        /// in some rare cases where the string defines the unit for the vector.
        /// </summary>
        Blob = 0x4000000,
    }

    public enum UFBXElementType
    {
        Unknown,            // < `ufbx_unknown`
        Node,               // < `ufbx_node`
        Mesh,               // < `ufbx_mesh`
        Light,              // < `ufbx_light`
        Camera,             // < `ufbx_camera`
        Bone,               // < `ufbx_bone`
        Empty,              // < `ufbx_empty`
        LineCurve,          // < `ufbx_line_curve`
        NurbsCurve,         // < `ufbx_nurbs_curve`
        NurbsSurface,       // < `ufbx_nurbs_surface`
        NurbsTrimSurface,   // < `ufbx_nurbs_trim_surface`
        NurbsTrimBoundary,  // < `ufbx_nurbs_trim_boundary`
        ProceduralGeometry, // < `ufbx_procedural_geometry`
        StereoCamera,       // < `ufbx_stereo_camera`
        CameraSwitcher,     // < `ufbx_camera_switcher`
        Marker,             // < `ufbx_marker`
        LodGroup,           // < `ufbx_lod_group`
        SkinDeformer,       // < `ufbx_skin_deformer`
        SkinCluster,        // < `ufbx_skin_cluster`
        BlendDeformer,      // < `ufbx_blend_deformer`
        BlendChannel,       // < `ufbx_blend_channel`
        BlendShape,         // < `ufbx_blend_shape`
        CacheDeformer,      // < `ufbx_cache_deformer`
        CacheFile,          // < `ufbx_cache_file`
        Material,           // < `ufbx_material`
        Texture,            // < `ufbx_texture`
        Video,              // < `ufbx_video`
        Shader,             // < `ufbx_shader`
        ShaderBinding,      // < `ufbx_shader_binding`
        AnimStack,          // < `ufbx_anim_stack`
        AnimLayer,          // < `ufbx_anim_layer`
        AnimValue,          // < `ufbx_anim_value`
        AnimCurve,          // < `ufbx_anim_curve`
        DisplayLayer,       // < `ufbx_display_layer`
        SelectionSet,       // < `ufbx_selection_set`
        SelectionNode,      // < `ufbx_selection_node`
        Character,          // < `ufbx_character`
        Constraint,         // < `ufbx_constraint`
        AudioLayer,         // < `ufbx_audio_layer`
        AudioClip,          // < `ufbx_audio_clip`
        Pose,               // < `ufbx_pose`
        MetadataObject,     // < `ufbx_metadata_object`

        FirstAttrib = Mesh,
        LastAttrib = LodGroup,
    }

    /// <summary>
    /// Inherit type specifies how hierarchial node transforms are combined.
    /// This only affects the final scaling, as rotation and translation are always
    /// inherited correctly.
    /// NOTE: These don't map to `"InheritType"` property as there may be new ones for
    /// compatibility with various exporters.
    /// </summary>
    public enum UFBXInheritMode
    {
        /// <summary>
        /// Normal matrix composition of hierarchy: `R*S*r*s`.
        /// child.node_to_world = parent.node_to_world * child.node_to_parent;
        /// </summary>
        Normal,
        /// <summary>
        /// Ignore parent scale when computing the transform: `R*r*s`.
        ///  ufbx_transform t = node.local_transform;
        ///  t.translation *= parent.inherit_scale;
        ///  t.scale *= node.inherit_scale_node.inherit_scale;
        ///  child.node_to_world = parent.unscaled_node_to_world * t;
        /// Also known as "Segment scale compensate" in some software.
        /// </summary>
        IgnoreParentScale,
        /// <summary>
        /// Apply parent scale component-wise: `R*r*S*s`.
        ///  ufbx_transform t = node.local_transform;
        ///  t.translation *= parent.inherit_scale;
        ///  t.scale *= node.inherit_scale_node.inherit_scale;
        ///  child.node_to_world = parent.unscaled_node_to_world * t;
        /// </summary>
        ComponentwiseScale,
    }

    /// <summary>
    /// Axis used to mirror transformations for handedness conversion.
    /// </summary>
    public enum UFBXMirrorAxis
    {
        None,
        X,
        Y,
        Z,
    }

    public enum UFBXSubdivisionDisplayMode
    {
        Disabled,
        Hull,
        HullAndSmooth,
        Smooth,
    }

    public enum UFBXSubdivisionBoundary
    {
        Default,
        Legacy,
        SharpCorners,
        SharpNone,
        SharpBoundary,
        SharpInterior,
    }

    /// <summary>
    /// The kind of light source
    /// </summary>
    public enum UFBXLightType
    {
        /// <summary>
        /// Single point at local origin, at `node->world_transform.position`
        /// </summary>
        Point,
        /// <summary>
        /// Infinite directional light pointing locally towards `light->local_direction`
        /// For global: `ufbx_transform_direction(&amp;node->node_to_world, light->local_direction)`
        /// </summary>
        Directional,
        /// <summary>
        /// Cone shaped light towards `light->local_direction`, between `light->inner/outer_angle`.
        /// For global: `ufbx_transform_direction(&amp;node->node_to_world, light->local_direction)`
        /// </summary>
        Spot,
        /// <summary>
        /// Area light, shape specified by `light->area_shape`
        /// </summary>
        Area,
        /// <summary>
        /// Volumetric light source
        /// </summary>
        Volume,
    }

    /// <summary>
    /// How fast does the light intensity decay at a distance
    /// </summary>
    public enum UFBXLightDecay
    {
        /// <summary>
        /// 1 (no decay)
        /// </summary>
        None,
        /// <summary>
        /// 1 / d
        /// </summary>
        Linear,
        /// <summary>
        /// 1 / d^2 (physically accurate)
        /// </summary>
        Quadratic,
        /// <summary>
        /// 1 / d^3
        /// </summary>
        Cubic,
    }

    public enum UFBXLightAreaShape
    {
        Rectangle,
        Sphere,
    }

    public enum UFBXProjectionMode
    {
        /// <summary>
        /// Perspective projection.
        /// </summary>
        Perspective,
        /// <summary>
        /// Orthographic projection.
        /// </summary>
        Orthographic,
    }

    /// <summary>
    /// Method of specifying the rendering resolution from properties
    /// NOTE: Handled internally by ufbx, ignore unless you interpret `ufbx_props` directly!
    /// </summary>
    public enum UFBXAspectMode
    {
        /// <summary>
        /// No defined resolution
        /// </summary>
        WindowSize,
        /// <summary>
        /// `"AspectWidth"` and `"AspectHeight"` are relative to each other
        /// </summary>
        FixedRatio,
        /// <summary>
        /// `"AspectWidth"` and `"AspectHeight"` are both pixels
        /// </summary>
        FixedResolution,
        /// <summary>
        /// `"AspectWidth"` is pixels, `"AspectHeight"` is relative to width
        /// </summary>
        FixedWidth,
        /// <summary>
        /// "AspectHeight"` is pixels, `"AspectWidth"` is relative to height
        /// </summary>
        FixedHeight,
    }

    /// <summary>
    /// Method of specifying the field of view from properties
    /// NOTE: Handled internally by ufbx, ignore unless you interpret `ufbx_props` directly!
    /// </summary>
    public enum UFBXApertureMode
    {
        /// <summary>
        /// Use separate `"FieldOfViewX"` and `"FieldOfViewY"` as horizontal/vertical FOV angles
        /// </summary>
        HorizontalAndVertical,
        /// <summary>
        /// Use `"FieldOfView"` as horizontal FOV angle, derive vertical angle via aspect ratio
        /// </summary>
        Horizontal,
        /// <summary>
        /// Use `"FieldOfView"` as vertical FOV angle, derive horizontal angle via aspect ratio
        /// </summary>
        Vertical,
        /// <summary>
        /// Compute the field of view from the render gate size and focal length
        /// </summary>
        FocalLength
    }

    /// <summary>
    /// Method of specifying the render gate size from properties
    /// NOTE: Handled internally by ufbx, ignore unless you interpret `ufbx_props` directly!
    /// </summary>
    public enum UFBXGateFit
    {
        /// <summary>
        /// Use the film/aperture size directly as the render gate
        /// </summary>
        None,
        /// <summary>
        /// Fit the render gate to the height of the film, derive width from aspect ratio
        /// </summary>
        Vertical,
        /// <summary>
        /// Fit the render gate to the width of the film, derive height from aspect ratio
        /// </summary>
        Horizontal,
        /// <summary>
        /// Fit the render gate so that it is fully contained within the film gate
        /// </summary>
        Fill,
        /// <summary>
        /// Fit the render gate so that it fully contains the film gate
        /// </summary>
        Overscan,
        /// <summary>
        /// Stretch the render gate to match the film gate
        /// </summary>
        Stretch,
    }

    /// <summary>
    /// Camera film/aperture size defaults
    /// NOTE: Handled internally by ufbx, ignore unless you interpret `ufbx_props` directly!
    /// </summary>
    public enum UFBXApertureFormat
    {
        FormatCustom,
        Format16mmTheatrical,
        FormatSuper16mm,
        Format35mmAcademy,
        Format35mmTVProjection,
        Format35mmFullAperture,
        Format35mm185Projection,
        Format35mm35mmAnamorphic,
        Format70mmProjection,
        FormatVistaVision,
        FormatDynaVision,
        FormatImax,
    }

    public enum UFBXCoordinateAxis
    {
        PositiveX,
        NegativeX,
        PositiveY,
        NegativeY,
        PositiveZ,
        NegativeZ,
        Unknown,
    }

    public enum UFBXNurbsTopology
    {
        /// <summary>
        /// The endpoints are not connected.
        /// </summary>
        Open,
        /// <summary>
        /// Repeats first `ufbx_nurbs_basis.order - 1` control points after the end.
        /// </summary>
        Periodic,
        /// <summary>
        /// Repeats the first control point after the end.
        /// </summary>
        Closed,
    }

    public enum UFBXMarkerType
    {
        /// <summary>
        /// Unknown marker type
        /// </summary>
        Unknown,
        /// <summary>
        /// FK (Forward Kinematics) effector
        /// </summary>
        FKEffector,

        /// <summary>
        /// IK (Inverse Kinematics) effector
        /// </summary>
        IKEffector,
    }

    /// <summary>
    /// LOD level display mode.
    /// </summary>
    public enum UFBXLODDisplay
    {
        /// <summary>
        /// Display the LOD level if the distance is appropriate.
        /// </summary>
        UseLod,
        /// <summary>
        /// Always display the LOD level.
        /// </summary>
        Show,
        /// <summary>
        /// Never display the LOD level.
        /// </summary>
        Hide,
    }

    /// <summary>
    /// Method to evaluate the skinning on a per-vertex level
    /// </summary>
    public enum UFBXSkinningMethod
    {
        /// <summary>
        /// Linear blend skinning: Blend transformation matrices by vertex weights
        /// </summary>
        Linear,
        /// <summary>
        /// One vertex should have only one bone attached
        /// </summary>
        Rigid,
        /// <summary>
        /// Convert the transformations to dual quaternions and blend in that space
        /// </summary>
        DualQuaternion,
        /// <summary>
        /// Blend between `UFBX_SKINNING_METHOD_LINEAR` and `UFBX_SKINNING_METHOD_BLENDED_DQ_LINEAR`
        /// The blend weight can be found either per-vertex in `ufbx_skin_vertex.dq_weight`
        /// or in `ufbx_skin_deformer.dq_vertices/dq_weights` (indexed by vertex).
        /// </summary>
        BlendedDualQuaternionLinear,
    }

    public enum UFBXCacheFileFormat
    {
        /// <summary>
        /// Unknown cache file format
        /// </summary>
        Unknown,
        /// <summary>
        /// .pc2 Point cache file
        /// </summary>
        PC2,
        /// <summary>
        /// .mc/.mcx Maya cache file
        /// </summary>
        MC,
    }

    public enum UFBXCacheDataFormat
    {
        /// <summary>
        /// Unknown data format
        /// </summary>
        Unknown,
        /// <summary>
        /// `float data[]`
        /// </summary>
        RealFloat,
        /// <summary>
        /// `struct { float x, y, z; } data[]`
        /// </summary>
        Vec3Float,
        /// <summary>
        /// `double data[]`
        /// </summary>
        RealDouble,
        /// <summary>
        /// `struct { double x, y, z; } data[]`
        /// </summary>
        Vec3Double,
    }

    public enum UFBXCacheDataEncoding
    {
        /// <summary>
        /// Unknown data encoding
        /// </summary>
        Unknown,
        /// <summary>
        /// Contiguous little-endian array
        /// </summary>
        LittleEndian,
        /// <summary>
        /// Contiguous big-endian array
        /// </summary>
        BigEndian,
    }

    /// <summary>
    /// Known interpretations of geometry cache data.
    /// </summary>
    public enum UFBXCacheInterpretation
    {
        /// <summary>
        /// Unknown interpretation, see `ufbx_cache_channel.interpretation_name` for more information.
        /// </summary>
        Unknown,
        /// <summary>
        /// Generic "points" interpretation, FBX SDK default. Usually fine to interpret
	    /// as vertex positions if no other cache channels are specified.
        /// </summary>
        Points,
        /// <summary>
        /// Vertex positions.
        /// </summary>
        VertexPosition,
        /// <summary>
        /// Vertex normals.
        /// </summary>
        VertexNormal,
    }

    /// <summary>
    /// Shading model type
    /// </summary>
    public enum UFBXShaderType
    {
        /// <summary>
        /// Unknown shading model
        /// </summary>
        ShaderUnknown,
        /// <summary>
        /// FBX builtin diffuse material
        /// </summary>
        ShaderFBXLambert,
        /// <summary>
        /// FBX builtin diffuse+specular material
        /// </summary>
        ShaderFBXPhong,
        /// <summary>
        /// Open Shading Language standard surface
        /// https://github.com/Autodesk/standard-surface
        /// </summary>
        ShaderOSLStandardSurface,
        /// <summary>
        /// Arnold standard surface
        /// https://docs.arnoldrenderer.com/display/A5AFMUG/Standard+Surface
        /// </summary>
        ShaderArnoldStandardardSurface,
        /// <summary>
        /// 3ds Max Physical Material
        /// https://knowledge.autodesk.com/support/3ds-max/learn-explore/caas/CloudHelp/cloudhelp/2022/ENU/3DSMax-Lighting-Shading/files/GUID-C1328905-7783-4917-AB86-FC3CC19E8972-htm.html
        /// </summary>
        Shader3DSMaxPhysicalMaterial,
        /// <summary>
        /// 3ds Max PBR (Metal/Rough) material
        /// https://knowledge.autodesk.com/support/3ds-max/learn-explore/caas/CloudHelp/cloudhelp/2021/ENU/3DSMax-Lighting-Shading/files/GUID-A16234A5-6500-4662-8B20-A5EC9FE1B255-htm.html
        /// </summary>
        Shader3DSMaxPBRMetalRough,
        /// <summary>
        /// 3ds Max PBR (Spec/Gloss) material
        /// https://knowledge.autodesk.com/support/3ds-max/learn-explore/caas/CloudHelp/cloudhelp/2021/ENU/3DSMax-Lighting-Shading/files/GUID-18087194-B2A6-43EF-9B80-8FD1736FAE52-htm.html
        /// </summary>
        Shader3DSMaxPBRSpecGloss,
        /// <summary>
        /// 3ds glTF Material
        /// https://help.autodesk.com/view/3DSMAX/2023/ENU/?guid=GUID-7ABFB805-1D9F-417E-9C22-704BFDF160FA
        /// </summary>
        ShaderGLTFMaterial,
        /// <summary>
        /// 3ds OpenPBR Material
        /// https://help.autodesk.com/view/3DSMAX/2025/ENU/?guid=GUID-CD90329C-1E2B-4BBA-9285-3BB46253B9C2
        /// </summary>
        ShaderOpenPBRMaterial,
        /// <summary>
        /// Stingray ShaderFX shader graph.
        /// Contains a serialized `"ShaderGraph"` in `ufbx_props`.
        /// </summary>
        ShaderShaderFXGGraph,
        /// <summary>
        /// Variation of the FBX phong shader that can recover PBR properties like
        /// `metalness` or `roughness` from the FBX non-physical values.
        /// NOTE: Enable `ufbx_load_opts.use_blender_pbr_material`.
        /// </summary>
        ShaderBlenderPhong,
        /// <summary>
        /// Wavefront .mtl format shader (used by .obj files)
        /// </summary>
        ShaderWavefrontMTL,
    }

    /// <summary>
    /// FBX builtin material properties, matches maps in `ufbx_material_fbx_maps`
    /// </summary>
    public enum UFBXMaterialFBXMap
    {
        DiffuseFactor,
        DiffuseColor,
        SpecularFactor,
        SpecularColor,
        SpecularExponent,
        ReflectionFactor,
        ReflectionColor,
        TransparencyFactor,
        TransparencyColor,
        EmissionFactor,
        EmissionColor,
        AmbientFactor,
        AmbientColor,
        NormalMap,
        Bump,
        BumpFactor,
        DisplacementFactor,
        Displacement,
        VectorDisplacementFactor,
        VectorDisplacement,
    }

    /// <summary>
    /// Known PBR material properties, matches maps in `ufbx_material_pbr_maps`
    /// </summary>
    public enum UFBXMaterialPBRMap
    {
        BaseFactor,
        BaseColor,
        Roughness,
        Metalness,
        DiffuseRoughness,
        SpecularFactor,
        SpecularColor,
        SpecularIOR,
        SpecularAnisotropy,
        SpecularRotation,
        TransmissionFactor,
        TransmissionColor,
        TransmissionDepth,
        TransmissionScatter,
        TransmissionScaterAnisotrophy,
        TransmissionDispersion,
        TransmissionRoughness,
        TransmissionExtraRoughness,
        TransmissionPriority,
        TransmissionEnableInAOV,
        SubsurfaceFactor,
        SubsurfaceColor,
        SubsurfaceRadius,
        SubsurfaceScale,
        SubsurfaceAnisotropy,
        SubsurfaceTintColor,
        SubsurfaceType,
        SheenFactor,
        SheenColor,
        SheenRoughness,
        CoatFactor,
        CoatColor,
        CoatRoughness,
        CoatIOR,
        CoatAnisotropy,
        CoatRotation,
        CoatNormal,
        CoatAffectBaseColor,
        CoatAffectBaseRoughness,
        ThinFilmFactor,
        ThinFilmThickness,
        ThinFilmIOR,
        EmissionFactor,
        EmissionColor,
        Opacity,
        IndirectDiffuse,
        IndirectSpecular,
        NormalMap,
        TangentMap,
        DisplacementMap,
        MatteFactor,
        MatteColor,
        AmbientOcclusion,
        Glossiness,
        CoatGlossiness,
        TransmissionGlossiness,
    }

    /// <summary>
    /// Known material features
    /// </summary>
    public enum UFBXMaterialFeature
    {
        PBR,
        Metalness,
        Diffuse,
        Specular,
        Emission,
        Transmission,
        Coat,
        Sheen,
        Opacity,
        AmbientOcclusion,
        Matte,
        Unlit,
        IOR,
        DiffuseRoughness,
        TransmissionRoughness,
        Thinwalled,
        Caustics,
        ExitToBackground,
        InternalReflections,
        DoubleSided,
        RoughnessAsGlossiness,
        CoatRoughnessAsGlossiness,
        TransmissionRoughnessAsGlossiness,
    }

    public enum UFBXTextureType
    {
        /// <summary>
        /// Texture associated with an image file/sequence. `texture->filename` and
        /// and `texture->relative_filename` contain the texture's path. If the file
        /// has embedded content `texture->content` may hold `texture->content_size`
        /// bytes of raw image data.
        /// </summary>
        File,
        /// <summary>
        /// The texture consists of multiple texture layers blended together.
        /// </summary>
        Layered,
        /// <summary>
        /// Reserved as these _should_ exist in FBX files.
        /// </summary>
        Procedural,
        /// <summary>
        /// Node in a shader graph.
        /// Use `ufbx_texture.shader` for more information.
        /// </summary>
        Shader,
    }

    /// <summary>
    /// Blend modes to combine layered textures with, compatible with common blend
    /// mode definitions in many art programs. Simpler blend modes have equations
    /// specified below where `src` is the layer to composite over `dst`.
    /// See eg. https://www.w3.org/TR/2013/WD-compositing-1-20131010/#blendingseparable
    /// </summary>
    public enum UFBXBlendMode
    {
        /// <summary>
        /// `src` effects result alpha
        /// </summary>
        Translucent,
        /// <summary>
        /// `src + dst`
        /// </summary>
        Additive,
        /// <summary>
        /// `src * dst`
        /// </summary>
        Multiply,
        /// <summary>
        /// `2 * src * dst`
        /// </summary>
        Multiply2X,
        /// <summary>
        /// `src * src_alpha + dst * (1-src_alpha)`
        /// </summary>
        Over,
        /// <summary>
        /// `src` Replace the contents
        /// </summary>
        Replace,
        /// <summary>
        /// `random() + src_alpha >= 1.0 ? src : dst`
        /// </summary>
        Dissolve,
        /// <summary>
        /// `min(src, dst)`
        /// </summary>
        Darken,
        /// <summary>
        /// `src > 0 ? 1 - min(1, (1-dst) / src) : 0`
        /// </summary>
        ColorBurn,
        /// <summary>
        /// `src + dst - 1`
        /// </summary>
        LinearBurn,
        /// <summary>
        /// `value(src) < value(dst) ? src : dst`
        /// </summary>
        DarkerColor,
        /// <summary>
        /// `max(src, dst)`
        /// </summary>
        Lighten,
        /// <summary>
        /// `1 - (1-src)*(1-dst)`
        /// </summary>
        Screen,
        /// <summary>
        /// `src < 1 ? dst / (1 - src)` : (dst>0?1:0)`
        /// </summary>
        ColorDodge,
        /// <summary>
        /// `src + dst`
        /// </summary>
        LinearDodge,
        /// <summary>
        /// `value(src) > value(dst) ? src : dst`
        /// </summary>
        LighterColor,
        /// <summary>
        /// https://www.w3.org/TR/2013/WD-compositing-1-20131010/#blendingsoftlight
        /// </summary>
        SoftLight,
        /// <summary>
        /// https://www.w3.org/TR/2013/WD-compositing-1-20131010/#blendinghardlight
        /// </summary>
        HardLight,
        /// <summary>
        /// Combination of `COLOR_DODGE` and `COLOR_BURN`
        /// </summary>
        VividLight,
        /// <summary>
        /// Combination of `LINEAR_DODGE` and `LINEAR_BURN`
        /// </summary>
        LinearLight,
        /// <summary>
        /// Combination of `DARKEN` and `LIGHTEN`
        /// </summary>
        PinLight,
        /// <summary>
        /// Produces primary colors depending on similarity
        /// </summary>
        HardMix,
        /// <summary>
        /// `abs(src - dst)`
        /// </summary>
        Difference,
        /// <summary>
        /// `dst + src - 2 * src * dst`
        /// </summary>
        Exclusion,
        /// <summary>
        /// `dst - src`
        /// </summary>
        Subtract,
        /// <summary>
        /// `dst / src`
        /// </summary>
        Divide,
        /// <summary>
        /// Replace hue
        /// </summary>
        Hue,
        /// <summary>
        /// Replace saturation
        /// </summary>
        Saturation,
        /// <summary>
        /// Replace hue and saturation
        /// </summary>
        Color,
        /// <summary>
        /// Replace value
        /// </summary>
        Luminosity,
        /// <summary>
        /// Same as `HARD_LIGHT` but with `src` and `dst` swapped
        /// </summary>
        Overlay,
    }

    /// <summary>
    /// Blend modes to combine layered textures with, compatible with common blend
    /// </summary>
    public enum UFBXWrapMode
    {
        /// <summary>
        /// Repeat the texture past the [0,1] range
        /// </summary>
        Repeat,
        /// <summary>
        /// Clamp the normalized texture coordinates to [0,1]
        /// </summary>
        Clamp,
    }

    public enum UFBXShaderTextureType
    {
        Unknown,
        /// <summary>
        /// Select an output of a multi-output shader.
        /// HINT: If this type is used the `ufbx_shader_texture.main_texture` and
        /// `ufbx_shader_texture.main_texture_output_index` fields are set.
        /// </summary>
        SelectOutput,
        /// <summary>
        /// Open Shading Language (OSL) shader.
        /// https://github.com/AcademySoftwareFoundation/OpenShadingLanguage
        /// </summary>
        OSL,
    }

    /// <summary>
    /// Animation curve segment interpolation mode between two keyframes
    /// </summary>
    public enum UFBXInterpolation
    {
        /// <summary>
        /// Hold previous key value
        /// </summary>
        ConstantPrev,
        /// <summary>
        /// Hold next key value
        /// </summary>
        ConstantNext,
        /// <summary>
        /// Linear interpolation between two keys
        /// </summary>
        Linear,
        /// <summary>
        /// Cubic interpolation, see `ufbx_tangent`
        /// </summary>
        Cubic,
    }

    public enum UFBXExtrapolationMode
    {
        /// <summary>
        /// Use the value of the first/last keyframe
        /// </summary>
        Constant,
        /// <summary>
        /// Repeat the whole animation curve
        /// </summary>
        Repeat,
        /// <summary>
        /// Repeat with mirroring
        /// </summary>
        Mirror,
        /// <summary>
        /// Use the tangent of the last keyframe to linearly extrapolate
        /// </summary>
        Slope,
        /// <summary>
        /// Repeat the animation curve but connect the first and last keyframe values
        /// </summary>
        RepeatRelative,
    }

    /// <summary>
    /// Type of property constrain eg. position or look-at
    /// </summary>
    public enum UFBXConstraintType
    {
        Unknown,
        Aim,
        Parent,
        Poosition,
        Rotation,
        Scale,
        /// <summary>
        /// Inverse kinematic chain to a single effector `ufbx_constraint.ik_effector`
        /// `targets` optionally contains a list of pole targets!
        /// </summary>
        SingleChainIK,
    }

    /// <summary>
    /// Method to determine the up vector in aim constraints
    /// </summary>
    public enum UFBXConstraintAimUpType
    {
        /// <summary>
        /// Align the up vector to the scene global up vector
        /// </summary>
        Scene,
        /// <summary>
        /// Aim the up vector at `ufbx_constraint.aim_up_node`
        /// </summary>
        ToNode,
        /// <summary>
        /// Copy the up vector from `ufbx_constraint.aim_up_node`
        /// </summary>
        AlignNode,
        /// <summary>
        /// Use `ufbx_constraint.aim_up_vector` as the up vector
        /// </summary>
        Vector,
        /// <summary>
        /// Don't align the up vector to anything
        /// </summary>
        None,
    }

    /// <summary>
    /// Method to determine the up vector in aim constraints
    /// </summary>
    public enum UFBXConstraintIKPoleType
    {
        /// <summary>
        /// Use towards calculated from `ufbx_constraint.targets`
        /// </summary>
        Vector,
        /// <summary>
        /// Use `ufbx_constraint.ik_pole_vector` directly
        /// </summary>
        Node,
    }

    public enum UFBXExporter
    {
        Unknown,
        FBXSDK,
        BlenderBinary,
        BlenderASCII,
        MotionBuilder,
    }

    public enum UFBXFileFormat
    {
        /// <summary>
        /// Unknown file format
        /// </summary>
        Unknown,
        /// <summary>
        /// .fbx Kaydara/Autodesk FBX file
        /// </summary>
        FBX,
        /// <summary>
        /// .obj Wavefront OBJ file
        /// </summary>
        OBJ,
        /// <summary>
        /// .mtl Wavefront MTL (Material template library) file
        /// </summary>
        MTL
    }

    public enum UFBXWarningType
    {
        /// <summary>
        /// Missing external file file (for example .mtl for Wavefront .obj file or a
        /// geometry cache)
        /// </summary>
        MissingExternalFile,
        /// <summary>
        /// Loaded a Wavefront .mtl file derived from the filename instead of a proper
        /// `mtllib` statement.
        /// </summary>
        ImplicitMTL,
        /// <summary>
        /// Truncated array has been auto-expanded.
        /// </summary>
        TruncatedArray,
        /// <summary>
        /// Geometry data has been defined but has no data.
        /// </summary>
        MissingGeometryData,
        /// <summary>
        /// Duplicated connection between two elements that shouldn't have.
        /// </summary>
        DuplicateConnection,
        /// <summary>
        /// Vertex 'W' attribute length differs from main attribute.
        /// </summary>
        BadVertexWAttribute,
        /// <summary>
        /// Missing polygon mapping type.
        /// </summary>
        MissingPolygonMapping,
        /// <summary>
        /// Unsupported version, loaded but may be incorrect.
        /// If the loading fails `UFBX_ERROR_UNSUPPORTED_VERSION` is issued instead.
        /// </summary>
        UnsupportedVersion,
        /// <summary>
        /// Out-of-bounds index has been clamped to be in-bounds.
        /// HINT: You can use `ufbx_index_error_handling` to adjust behavior.
        /// </summary>
        IndexClamped,
        /// <summary>
        /// Non-UTF8 encoded strings.
        /// HINT: You can use `ufbx_unicode_error_handling` to adjust behavior.
        /// </summary>
        BadUnicode,
        /// <summary>
        /// Invalid base64-encoded embedded content ignored.
        /// </summary>
        BadBase64Content,
        /// <summary>
        /// Non-node element connected to root.
        /// </summary>
        BadElementConnectedToRoot,
        /// <summary>
        /// Duplicated object ID in the file, connections will be wrong.
        /// </summary>
        DuplicateObjectID,
        /// <summary>
        /// Empty face has been removed.
        /// Use `ufbx_load_opts.allow_empty_faces` if you want to allow them.
        /// </summary>
        EmptyFaceRemoved,
        /// <summary>
        /// Unknown .obj file directive.
        /// </summary>
        UnknownOBJDirective,
        /// <summary>
        /// Warnings after this one are deduplicated.
        /// See `ufbx_warning.count` for how many times they happened.
        /// </summary>
        TypeFirstDeduplicated = IndexClamped,
    }

    public enum UFBXThumbnailFormat
    {
        /// <summary>
        /// Unknown format
        /// </summary>
        Unknown,
        /// <summary>
        /// 8-bit RGB pixels, in memory R,G,B
        /// </summary>
        RGB24,
        /// <summary>
        /// 8-bit RGBA pixels, in memory R,G,B,A
        /// </summary>
        RGBA32,
    }

    /// <summary>
    /// Specify how unit / coordinate system conversion should be performed.
    /// Affects how `ufbx_load_opts.target_axes` and `ufbx_load_opts.target_unit_meters` work,
    /// has no effect if neither is specified.
    /// </summary>
    public enum UFBXSpaceConversion
    {
        /// <summary>
        /// Store the space conversion transform in the root node.
        /// Sets `ufbx_node.local_transform` of the root node.
        /// </summary>
        TransformRoot,
        /// <summary>
        /// Perform the conversion by using "adjust" transforms.
        /// Compensates for the transforms using `ufbx_node.adjust_pre_rotation` and
        /// `ufbx_node.adjust_pre_scale`. You don't need to account for these unless
        /// you are manually building transforms from `ufbx_props`.
        /// </summary>
        AdjustTransforms,
        /// <summary>
        /// Perform the conversion by scaling geometry in addition to adjusting transforms.
        /// Compensates transforms like `UFBX_SPACE_CONVERSION_ADJUST_TRANSFORMS` but
        /// applies scaling to geometry as well.
        /// </summary>
        ModifyGeometry,
    }

    public enum UFBXTimeMode
    {
        ModeDefault,
        Mode120FPS,
        Mode100FPS,
        Mode60FPS,
        Mode50FPS,
        Mode48FPS,
        Mode30FPS,
        Mode30FPSDrop,
        ModeNTSCDropFrame,
        ModeNTSCFullFrame,
        ModePAL,
        Mode24FPS,
        Mode1000FPS,
        ModeFilmFullFrame,
        ModeCustom,
        Mode96FPS,
        Mode72FPS,
        Mode5994FPS,
    }

    public enum UFBXTimeProtocol
    {
        SMPTE,
        FrameCount,
        Default,
    }

    public enum UFBXSnapMode
    {
        None,
        Snap,
        Play,
        SnapAndPlay,
    }

    [Flags]
    public enum UFBXTopoFlags
    {
        /// <summary>
        /// Edge with three or more faces
        /// </summary>
        NonManifold = 0x1,
    }

    /// <summary>
    /// Error causes (and `UFBX_ERROR_NONE` for no error).
    /// </summary>
    public enum UFBXErrorType
    {
        /// <summary>
        /// No error, operation has been performed successfully.
        /// </summary>
        None,
        /// <summary>
        /// Unspecified error, most likely caused by an invalid FBX file or a file
        /// that contains something ufbx can't handle.
        /// </summary>
        Unknown,
        /// <summary>
        /// File not found.
        /// </summary>
        FileNotFound,
        /// <summary>
        /// Empty file.
        /// </summary>
        EmptyFile,
        /// <summary>
        /// External file not found.
        /// See `ufbx_load_opts.load_external_files` for more information.
        /// </summary>
        ExternalFileNotFound,
        /// <summary>
        /// Out of memory (allocator returned `NULL`).
        /// </summary>
        OutOfMemory,
        /// <summary>
        /// `ufbx_allocator_opts.memory_limit` exhausted.
        /// </summary>
        MemoryLimit,
        /// <summary>
        /// `ufbx_allocator_opts.allocation_limit` exhausted.
        /// </summary>
        AllocationLimit,
        /// <summary>
        /// File ended abruptly.
        /// </summary>
        TruncatedFile,
        /// <summary>
        /// IO read error.
        /// eg. returning `SIZE_MAX` from `ufbx_stream.read_fn` or stdio `ferror()` condition.
        /// </summary>
        IO,
        /// <summary>
        /// User cancelled the loading via `ufbx_load_opts.progress_cb` returning `UFBX_PROGRESS_CANCEL`.
        /// </summary>
        Cancelled,
        /// <summary>
        /// Could not detect file format from file data or filename.
        /// HINT: You can supply it manually using `ufbx_load_opts.file_format` or use `ufbx_load_opts.filename`
        /// when using `ufbx_load_memory()` to let ufbx guess the format from the extension.
        /// </summary>
        UnrecognizedFileFormat,
        /// <summary>
        /// Options struct (eg. `ufbx_load_opts`) is not cleared to zero.
        /// Make sure you initialize the structure to zero via eg.
        ///   ufbx_load_opts opts = { 0 }; // C
        ///   ufbx_load_opts opts = { }; // C++
        /// </summary>
        UninitializedOptions,
        /// <summary>
        /// The vertex streams in `ufbx_generate_indices()` are empty.
        /// </summary>
        ZeroVertexSize,
        /// <summary>
        /// Vertex stream passed to `ufbx_generate_indices()`.
        /// </summary>
        TruncatedVertexStream,
        /// <summary>
        /// Invalid UTF-8 encountered in a file when loading with `UFBX_UNICODE_ERROR_HANDLING_ABORT_LOADING`.
        /// </summary>
        InvalidUTF8,
        /// <summary>
        /// Feature needed for the operation has been compiled out.
        /// </summary>
        FeatureDisabled,
        /// <summary>
        /// Attempting to tessellate an invalid NURBS object.
        /// See `ufbx_nurbs_basis.valid`.
        /// </summary>
        BadNURBS,
        /// <summary>
        /// Out of bounds index in the file when loading with `UFBX_INDEX_ERROR_HANDLING_ABORT_LOADING`.
        /// </summary>
        BadIndex,
        /// <summary>
        /// Node is deeper than `ufbx_load_opts.node_depth_limit` in the hierarchy.
        /// </summary>
        NodeDepthLimit,
        /// <summary>
        /// Error parsing ASCII array in a thread.
        /// Threaded ASCII parsing is slightly more strict than non-threaded, for cursed files,
        /// set `ufbx_load_opts.force_single_thread_ascii_parsing` to `true`.
        /// </summary>
        ThreadedASCIIParse,
        /// <summary>
        /// Unsafe options specified without enabling `ufbx_load_opts.allow_unsafe`.
        /// </summary>
        UnsafeOptions,
        /// <summary>
        /// Duplicated override property in `ufbx_create_anim()`
        /// </summary>
        DuplicateOverride,
        /// <summary>
        /// Unsupported file format version.
        /// ufbx still tries to load files with unsupported versions, see `UFBX_WARNING_UNSUPPORTED_VERSION`.
        /// </summary>
        UnsupportedVersion,
    }

    /// <summary>
    /// Progress result returned from `ufbx_progress_fn()` callback.
    /// Determines whether ufbx should continue or abort the loading.
    /// </summary>
    [Flags]
    public enum UFBXProgressResult
    {
        Continue = 0x100,
        Cancel = 0x200,
    }

    public enum UFBXOpenFileType
    {
        /// <summary>
        /// Main model file
        /// </summary>
        MainModel,
        /// <summary>
        /// Unknown geometry cache file
        /// </summary>
        GeometryCache,
        /// <summary>
        /// .mtl material library file
        /// </summary>
        ObjMtl,
    }

    /// <summary>
    /// Null-terminated UTF-8 encoded string within an FBX file
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXString
    {
        public nint data;
        public ulong length;

        public readonly string AsString
        {
            get
            {
                if (data == nint.Zero)
                {
                    return "";
                }

                return Encoding.UTF8.GetString((byte*)data, (int)length);
            }
        }

        public override readonly string ToString() => AsString;

        public static implicit operator string(UFBXString str) => str.AsString;
    }

    /// <summary>
    /// Opaque byte buffer blob
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXBlob
    {
        public nint data;
        public ulong length;
    }

    /// <summary>
    /// Explicit translation+rotation+scale transformation.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXTransform
    {
        public Vector3 translation;
        public Quaternion rotation;
        public Vector3 scale;
    }

    /// <summary>
    /// 4x3 matrix encoding an affine transformation.
    /// `cols[0..2]` are the X/Y/Z basis vectors, `cols[3]` is the translation
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXMatrix
    {
        public Vector3 column0;
        public Vector3 column1;
        public Vector3 column2;
        public Vector3 column3;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXVoidList
    {
        public nint data;
        public ulong count;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXList<T>
    {
        public nint data;
        public ulong count;

        public readonly T[] Value
        {
            get
            {
                if (data == nint.Zero)
                {
                    return [];
                }

                unsafe
                {
                    var array = new T[count];

                    var span = new Span<T>((T*)data, (int)count);

                    var target = new Span<T>(array);

                    span.CopyTo(target);

                    return array;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXBoolList
    {
        public nint data;
        public ulong count;

        public readonly bool[] Value
        {
            get
            {
                if (data == nint.Zero)
                {
                    return [];
                }

                unsafe
                {
                    var array = new bool[count];

                    var span = new Span<byte>((byte*)data, (int)count);

                    for (var i = 0; i < (int)count; i++)
                    {
                        array[i] = span[i] != 0;
                    }

                    return array;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXStringList
    {
        public nint data;
        public ulong count;

        public readonly string[] Value
        {
            get
            {
                if (data == nint.Zero || count < 0)
                {
                    return [];
                }

                unsafe
                {
                    var array = new string[count];

                    var span = new Span<UFBXString>((UFBXString*)data, (int)count);

                    for (var i = 0; i < (int)count; i++)
                    {
                        array[i] = span[i];
                    }

                    return array;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXDomValue
    {
        public UFBXDomValueType type;
        public UFBXString stringValue;
        public UFBXBlob blobValue;
        public long intValue;
        public double floatValue;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXDomNode
    {
        public UFBXString name;
        public UFBXDomNodeList children;
        public UFBXList<UFBXDomValue> values;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXDomNodeList
    {
        public nint data;
        public ulong count;

        public readonly UFBXDomNode[] Value
        {
            get
            {
                if (data == nint.Zero || count < 0)
                {
                    return [];
                }

                unsafe
                {
                    var array = new UFBXDomNode[count];

                    var span = new Span<UFBXDomNode>((UFBXDomNode*)data, (int)count);

                    var target = new Span<UFBXDomNode>(array);

                    span.CopyTo(target);

                    return array;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXProp
    {
        public UFBXString name;

        public uint _internal_key;

        public UFBXPropType type;
        public UFBXPropFlags flags;

        public UFBXString stringValue;
        public UFBXBlob blobValue;
        public long intValue;
        public float realValue0;
        public float realValue1;
        public float realValue2;
        public float realValue3;

        public readonly float realValue => realValue0;

        public readonly Vector2 Vector2Value => new(realValue0, realValue1);

        public readonly Vector3 Vector3Value => new(realValue0, realValue1, realValue2);

        public readonly Vector4 Vector4Value => new(realValue0, realValue1, realValue2, realValue3);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXProps
    {
        public UFBXList<UFBXProp> props;
        public ulong numAnimated;

        public nint defaults;

        public readonly bool TryGetDefaults(out UFBXProps props)
        {
            if (defaults == nint.Zero)
            {
                props = default;

                return false;
            }

            unsafe
            {
                props = *(UFBXProps*)defaults;
            }

            return true;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXConnection
    {
        public UFBXElement* src;
        public UFBXElement* dst;
        public UFBXString srcProp;
        public UFBXString UFBXString;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXUnknown
    {
        public UFBXElement element;

        public UFBXString type;
        public UFBXString superType;
        public UFBXString subType;
    }

    /// <summary>
    /// Nodes form the scene transformation hierarchy and can contain attached
    /// elements such as meshes or lights. In normal cases a single `ufbx_node`
    /// contains only a single attached element, so using `type/mesh/...` is safe.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXNode
    {
        public UFBXElement element;

        /// <summary>
        /// Parent node containing this one if not root.
        ///
        /// Always non-`NULL` for non-root nodes unless
        /// `ufbx_load_opts.allow_nodes_out_of_root` is enabled.
        /// </summary>
        public UFBXNode* parent;

        /// <summary>
        /// List of child nodes parented to this node.
        /// </summary>
        public UFBXList<UFBXNode> children;

        /// <summary>
        /// Common attached element type and typed pointers. Set to `NULL` if not in
        /// use, so checking `attrib_type` is not required.
        ///
        /// HINT: If you need less common attributes access `ufbx_node.attrib`, you
        /// can use utility functions like `ufbx_as_nurbs_curve(attrib)` to convert
        /// and check the attribute in one step.
        /// </summary>
        public UFBXMesh* mesh;
        public UFBXLight* light;
        public UFBXCamera* camera;
        public UFBXBone* bone;

        /// <summary>
        /// Less common attributes use these fields.
        ///
        /// Defined even if it is one of the above, eg. `ufbx_mesh`. In case there
        /// is multiple attributes this will be the first one.
        /// </summary>
        public UFBXElement* attrib;

        /// <summary>
        /// Geometry transform helper if one exists.
        /// See `UFBX_GEOMETRY_TRANSFORM_HANDLING_HELPER_NODES`.
        /// </summary>
        public UFBXNode* geometryTransformHelper;

        /// <summary>
        /// Scale helper if one exists.
        /// See `UFBX_INHERIT_MODE_HANDLING_HELPER_NODES`.
        /// </summary>
        public UFBXNode* scaleHelper;

        /// <summary>
        /// `attrib->type` if `attrib` is defined, otherwise `UFBX_ELEMENT_UNKNOWN`.
        /// </summary>
        public UFBXElementType attribType;

        /// <summary>
        /// List of _all_ attached attribute elements.
        ///
        /// In most cases there is only zero or one attributes per node, but if you
        /// have a very exotic FBX file nodes may have multiple attributes.
        /// </summary>
        public UFBXList<UFBXElement> allAttribs;

        /// <summary>
        /// Local transform in parent, geometry transform is a non-inherited
        /// transform applied only to attachments like meshes
        /// </summary>
        public UFBXInheritMode inheritMode;
        public UFBXInheritMode originalInheritMode;

        public UFBXTransform localTransform;
        public UFBXTransform geometryTransform;

        /// <summary>
        /// Combined scale when using `UFBX_INHERIT_MODE_COMPONENTWISE_SCALE`.
        /// Contains `local_transform.scale` otherwise.
        /// </summary>
        public Vector3 inheritScale;

        /// <summary>
        /// Node where scale is inherited from for `UFBX_INHERIT_MODE_COMPONENTWISE_SCALE`
        /// and even for `UFBX_INHERIT_MODE_IGNORE_PARENT_SCALE`.
        /// For componentwise-scale nodes, this will point to `parent`, for scale ignoring
        /// nodes this will point to the parent of the nearest componentwise-scaled node
        /// in the parent chain.
        /// </summary>
        public UFBXNode* inheritScaleNode;

        /// <summary>
        /// Specifies the axis order `euler_rotation` is applied in.
        /// </summary>
        public UFBXRotationOrder rotationOrder;

        /// <summary>
        /// Rotation around the local X/Y/Z axes in `rotation_order`.
        /// The angles are specified in degrees.
        /// </summary>
        public Vector3 eulerRotation;

        // Matrices derived from the transformations, for transforming geometry
        // prefer using `geometry_to_world` as that supports geometric transforms.

        /// <summary>
        /// Transform from this node to `parent` space.
        /// Equivalent to `ufbx_transform_to_matrix(&amp;local_transform)`.
        /// </summary>
        public UFBXMatrix nodeToParent;

        /// <summary>
        /// Transform from this node to the world space, ie. multiplying all the
        /// `node_to_parent` matrices of the parent chain together.
        /// </summary>
        public UFBXMatrix nodeToWorld;

        /// <summary>
        /// Transform from the attribute to this node. Does not affect the transforms
        /// of `children`!
        /// Equivalent to `ufbx_transform_to_matrix(&amp;geometry_transform)`.
        /// </summary>
        public UFBXMatrix geometryToNode;

        /// <summary>
        /// Transform from attribute space to world space.
        /// Equivalent to `ufbx_matrix_mul(&amp;node_to_world, &amp;geometry_to_node)`.
        /// </summary>
        public UFBXMatrix geometryToWorld;

        /// <summary>
        /// Transform from this node to world space, ignoring self scaling.
        /// </summary>
        public UFBXMatrix unscaledNodeToWorld;

        /// ufbx-specific adjustment for switching between coodrinate/unit systems.
        /// HINT: In most cases you don't need to deal with these as these are baked
        /// into all the transforms above and into `ufbx_evaluate_transform()`.

        /// <summary>
        /// ufbx-specific adjustment for switching between coodrinate/unit systems.
        /// </summary>
        public Vector3 adjustPreTranslatioon;

        /// <summary>
        /// Rotation applied between parent and self
        /// </summary>
        public Quaternion adjustPreRotation;
        /// <summary>
        /// Scaling applied between parent and self
        /// </summary>
        public float adjustPreScale;
        /// <summary>
        /// Rotation applied in local space at the end
        /// </summary>
        public Quaternion adjustPostRotation;
        /// <summary>
        /// Scaling applied in local space at the end
        /// </summary>
        public float adjustPostScale;
        /// <summary>
        /// Scaling applied to translation only
        /// </summary>
        public float adjustTranslationScale;
        /// <summary>
        /// Mirror translation and rotation on this axis
        /// </summary>
        public UFBXMirrorAxis adjustMirrorAxis;

        /// <summary>
        /// Materials used by `mesh` or other `attrib`.
        /// There may be multiple copies of a single `ufbx_mesh` with different materials
        /// in the `ufbx_node` instances.
        /// </summary>
        public UFBXList<UFBXMaterial> materials;

        /// <summary>
        /// Bind pose
        /// </summary>
        public UFBXPose* bindPose;

        /// <summary>
        /// Visibility state.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool visible;

        /// <summary>
        /// True if this node is the implicit root node of the scene.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool IsRoot;

        /// <summary>
        /// True if the node has a non-identity `geometry_transform`.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool HasGeometryTransform;

        /// <summary>
        /// If `true` the transform is adjusted by ufbx, not enabled by default.
        /// See `adjust_pre_rotation`, `adjust_pre_scale`, `adjust_post_rotation`,
        /// and `adjust_post_scale`.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool HasAdjustTransform;

        /// <summary>
        /// Scale is adjusted by root scale.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool HasRootAdjustTransform;

        /// <summary>
        /// True if this node is a synthetic geometry transform helper.
        /// See `UFBX_GEOMETRY_TRANSFORM_HANDLING_HELPER_NODES`.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool IsGeometryTransformHelper;

        /// <summary>
        /// True if the node is a synthetic scale compensation helper.
        /// See `UFBX_INHERIT_MODE_HANDLING_HELPER_NODES`.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool IsScaleHelper;

        /// <summary>
        /// Parent node to children that can compensate for parent scale.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool IsScaleCompensateParent;

        /// <summary>
        /// How deep is this node in the parent hierarchy. Root node is at depth `0`
        /// and the immediate children of root at `1`.
        /// </summary>
        public uint nodeDepth;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXVertexAttrib
    {
        /// <summary>
        /// Is this attribute defined by the mesh.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool exists;

        /// <summary>
        /// List of values the attribute uses.
        /// </summary>
        public UFBXVoidList values;

        /// <summary>
        /// Indices into `values[]`, indexed up to `ufbx_mesh.num_indices`.
        /// </summary>
        public UFBXList<uint> indices;

        /// <summary>
        /// Number of `ufbx_real` entries per value.
        /// </summary>
        public ulong valueReals;

        /// <summary>
        /// `true` if this attribute is defined per vertex, instead of per index.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool uniquePerVertex;

        /// <summary>
        /// Optional 4th 'W' component for the attribute.
        /// May be defined for the following:
        ///   ufbx_mesh.vertex_normal
        ///   ufbx_mesh.vertex_tangent / ufbx_uv_set.vertex_tangent
        ///   ufbx_mesh.vertex_bitangent / ufbx_uv_set.vertex_bitangent
        /// NOTE: This is not loaded by default, set `ufbx_load_opts.retain_vertex_attrib_w`.
        /// </summary>
        public UFBXList<float> values_w;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXVertexReal
    {
        [MarshalAs(UnmanagedType.I1)]
        public bool exists;
        public UFBXList<float> values;
        public UFBXList<uint> indices;
        public ulong valueReals;
        [MarshalAs(UnmanagedType.I1)]
        public bool uniquePerVertex;
        public UFBXList<float> values_w;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXVertexVector2
    {
        [MarshalAs(UnmanagedType.I1)]
        public bool exists;
        public UFBXList<Vector2> values;
        public UFBXList<uint> indices;
        public ulong valueReals;
        [MarshalAs(UnmanagedType.I1)]
        public bool uniquePerVertex;
        public UFBXList<float> values_w;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXVertexVector3
    {
        [MarshalAs(UnmanagedType.I1)]
        public bool exists;
        public UFBXList<Vector3> values;
        public UFBXList<uint> indices;
        public ulong valueReals;
        [MarshalAs(UnmanagedType.I1)]
        public bool uniquePerVertex;
        public UFBXList<float> values_w;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXVertexVector4
    {
        [MarshalAs(UnmanagedType.I1)]
        public bool exists;
        public UFBXList<Vector4> values;
        public UFBXList<uint> indices;
        public ulong valueReals;
        [MarshalAs(UnmanagedType.I1)]
        public bool uniquePerVertex;
        public UFBXList<float> values_w;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXUVSet
    {
        public UFBXString name;
        public uint index;

        /// <summary>
        /// UV / texture coordinates
        /// </summary>
        public UFBXVertexVector2 uv;
        /// <summary>
        /// (optional) Tangent vector in UV.x direction
        /// </summary>
        public UFBXVertexVector3 tangent;
        /// <summary>
        /// (optional) Tangent vector in UV.y direction
        /// </summary>
        public UFBXVertexVector3 bitangent;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXColorSet
    {
        public UFBXString name;
        public uint index;

        /// <summary>
        /// Per-vertex RGBA color
        /// </summary>
        public UFBXVertexVector4 color;
    }

    /// <summary>
    /// Edge between two _indices_ in a mesh
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXEdge
    {
        public uint a, b;
    }

    /// <summary>
    /// Polygonal face with arbitrary number vertices, a single face contains a
    /// contiguous range of mesh indices, eg. `{5,3}` would have indices 5, 6, 7
    ///
    /// NOTE: `num_indices` maybe less than 3 in which case the face is invalid!
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXFace
    {
        public uint startIndex;
        public uint indexCount;
    }

    /// <summary>
    /// Subset of mesh faces used by a single material or group.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXMeshPart
    {
        /// <summary>
        /// Index of the mesh part.
        /// </summary>
        public uint index;

        /// <summary>
        /// Number of faces (polygons)
        /// </summary>
        public ulong faceCount;

        /// <summary>
        /// Number of triangles if triangulated
        /// </summary>
        public ulong triangleCount;

        /// <summary>
        /// Number of faces with zero vertices
        /// </summary>
        public ulong emptyFaceCount;

        /// <summary>
        /// Number of faces with a single vertex
        /// </summary>
        public ulong pointFaceCount;

        /// <summary>
        /// Number of faces with two vertices
        /// </summary>
        public ulong lineFaceCount;

        /// <summary>
        /// Indices to `ufbx_mesh.faces[]`.
        /// Always contains `num_faces` elements.
        /// </summary>
        public UFBXList<uint> faceIndices;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXFaceGroup
    {
        /// <summary>
        /// Numerical ID for this group.
        /// </summary>
        public int ID;

        /// <summary>
        /// Name for the face group.
        /// </summary>
        public UFBXString name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXSubdivisionWeightRange
    {
        public uint weightStart;
        public uint weightCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXSubdivisionWeight
    {
        public float weight;
        public uint index;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXSubdivisionResult
    {
        public ulong resultMemoryUsed;
        public ulong tempMemoryUsed;
        public ulong resultAllocs;
        public ulong tempAllocs;

        /// <summary>
        /// Weights of vertices in the source model.
        /// Defined if `ufbx_subdivide_opts.evaluate_source_vertices` is set.
        /// </summary>
        public UFBXList<UFBXSubdivisionWeightRange> sourceVertexRanges;
        /// <summary>
        /// Weights of vertices in the source model.
        /// Defined if `ufbx_subdivide_opts.evaluate_source_vertices` is set.
        /// </summary>
        public UFBXList<UFBXSubdivisionWeight> sourceVertexWeights;

        /// <summary>
        /// Weights of skin clusters in the source model.
        /// Defined if `ufbx_subdivide_opts.evaluate_skin_weights` is set.
        /// </summary>
        public UFBXList<UFBXSubdivisionWeightRange> skinClusterRanges;
        /// <summary>
        /// Weights of skin clusters in the source model.
        /// Defined if `ufbx_subdivide_opts.evaluate_skin_weights` is set.
        /// </summary>
        public UFBXList<UFBXSubdivisionWeight> skinClusterWeights;
    }

    // Polygonal mesh geometry.
    //
    // Example mesh with two triangles (x, z) and a quad (y).
    // The faces have a constant UV coordinate x/y/z.
    // The vertices have _per vertex_ normals that point up/down.
    //
    //     ^   ^     ^
    //     A---B-----C
    //     |x /     /|
    //     | /  y  / |
    //     |/     / z|
    //     D-----E---F
    //     v     v   v
    //
    // Attributes may have multiple values within a single vertex, for example a
    // UV seam vertex has two UV coordinates. Thus polygons are defined using
    // an index that counts each corner of each face polygon. If an attribute is
    // defined (even per-vertex) it will always have a valid `indices` array.
    //
    //   {0,3}    {3,4}    {7,3}   faces ({ index_begin, num_indices })
    //   0 1 2   3 4 5 6   7 8 9   index
    //
    //   0 1 3   1 2 4 3   2 4 5   vertex_indices[index]
    //   A B D   B C E D   C E F   vertices[vertex_indices[index]]
    //
    //   0 0 1   0 0 1 1   0 1 1   vertex_normal.indices[index]
    //   ^ ^ v   ^ ^ v v   ^ v v   vertex_normal.data[vertex_normal.indices[index]]
    //
    //   0 0 0   1 1 1 1   2 2 2   vertex_uv.indices[index]
    //   x x x   y y y y   z z z   vertex_uv.data[vertex_uv.indices[index]]
    //
    // Vertex position can also be accessed uniformly through an accessor:
    //   0 1 3   1 2 4 3   2 4 5   vertex_position.indices[index]
    //   A B D   B C E D   C E F   vertex_position.data[vertex_position.indices[index]]
    //
    // Some geometry data is specified per logical vertex. Vertex positions are
    // the only attribute that is guaranteed to be defined _uniquely_ per vertex.
    // Vertex attributes _may_ be defined per vertex if `unique_per_vertex == true`.
    // You can access the per-vertex values by first finding the first index that
    // refers to the given vertex.
    //
    //   0 1 2 3 4 5  vertex
    //   A B C D E F  vertices[vertex]
    //
    //   0 1 4 2 5 9  vertex_first_index[vertex]
    //   0 0 0 1 1 1  vertex_normal.indices[vertex_first_index[vertex]]
    //   ^ ^ ^ v v v  vertex_normal.data[vertex_normal.indices[vertex_first_index[vertex]]]
    //
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXMesh
    {
        public UFBXElement element;

        // Number of "logical" vertices that would be treated as a single point,
        // one vertex may be split to multiple indices for split attributes, eg. UVs

        /// <summary>
        /// Number of logical "vertex" points
        /// </summary>
        public ulong vertexCount;

        /// <summary>
        /// Number of combiend vertex/attribute tuples
        /// </summary>
        public ulong indexCount;

        /// <summary>
        /// Number of faces (polygons) in the mesh
        /// </summary>
        public ulong faceCount;

        /// <summary>
        /// Number of triangles if triangulated
        /// </summary>
        public ulong triangleCount;

        /// <summary>
        /// Number of edges in the mesh.
        /// NOTE: May be zero in valid meshes if the file doesn't contain edge adjacency data!
        /// </summary>
        public ulong edgeCount;

        /// <summary>
        /// Maximum number of triangles in a  face in this mesh
        /// </summary>
        public ulong maxFaceTriangles;

        /// <summary>
        /// Number of faces with zero vertices
        /// </summary>
        public ulong emptyFaceCount;

        /// <summary>
        /// Number of faces with a single vertex
        /// </summary>
        public ulong pointFaceCount;

        /// <summary>
        /// Number of faces with two vertices
        /// </summary>
        public ulong lineFaceCount;

        /// <summary>
        /// Face index range
        /// </summary>
        public UFBXList<UFBXFace> faces;

        /// <summary>
        /// Should the face have soft normals
        /// </summary>
        public UFBXBoolList faceSmoothing;

        /// <summary>
        /// Indices to `ufbx_mesh.materials[]` and `ufbx_node.materials[]`
        /// </summary>
        public UFBXList<uint> faceMaterial;

        /// <summary>
        /// Face polygon group index, indices to `ufbx_mesh.face_groups[]`
        /// </summary>
        public UFBXList<uint> faceGroup;

        /// <summary>
        /// Should the face be hidden as a "hole"
        /// </summary>
        public UFBXBoolList faceHole;

        /// <summary>
        /// Edge index range
        /// </summary>
        public UFBXList<UFBXEdge> edges;

        /// <summary>
        /// Should the edge have soft normals
        /// </summary>
        public UFBXBoolList edgeSmoothing;

        /// <summary>
        /// Crease value for subdivision surfaces
        /// </summary>
        public UFBXList<float> edgeCrease;

        /// <summary>
        /// Should the edge be visible
        /// </summary>
        public UFBXBoolList edgeVisibility;

        /// <summary>
        /// Logical vertices and positions, alternatively you can use
        /// `vertex_position` for consistent interface with other attributes.
        /// </summary>
        public UFBXList<uint> vertexIndices;

        public UFBXList<Vector3> vertices;

        /// <summary>
        /// First index referring to a given vertex, `UFBX_NO_INDEX` if the vertex is unused.
        /// </summary>
        public UFBXList<uint> vertexFirstIndex;

        // Vertex attributes, see the comment over the struct.
        //
        // NOTE: Not all meshes have all attributes, in that case `indices/data == NULL`!
        //
        // NOTE: UV/tangent/bitangent and color are the from first sets,
        // use `uv_sets/color_sets` to access the other layers.

        /// <summary>
        /// Vertex positions
        /// </summary>
        public UFBXVertexVector3 vertexPosition;

        /// <summary>
        /// (optional) Normal vectors, always defined if `ufbx_load_opts.generate_missing_normals`
        /// </summary>
        public UFBXVertexVector3 vertexNormal;

        /// <summary>
        /// (optional) UV / texture coordinates
        /// </summary>
        public UFBXVertexVector2 vertexUV;

        /// <summary>
        /// (optional) Tangent vector in UV.x direction
        /// </summary>
        public UFBXVertexVector3 vertexTangent;

        /// <summary>
        /// (optional) Tangent vector in UV.y direction
        /// </summary>
        public UFBXVertexVector3 vertexBitangent;

        /// <summary>
        /// (optional) Per-vertex RGBA color
        /// </summary>
        public UFBXVertexVector4 vertexColor;

        /// <summary>
        /// (optional) Crease value for subdivision surfaces
        /// </summary>
        public UFBXVertexReal vertexCrease;

        // Multiple named UV/color sets
        // NOTE: The first set contains the same data as `vertex_uv/color`!

        public UFBXList<UFBXUVSet> uvSets;
        public UFBXList<UFBXColorSet> colorSets;

        /// <summary>
        /// Materials used by the mesh.
        /// NOTE: These can be wrong if you want to support per-instance materials!
        /// Use `ufbx_node.materials[]` to get the per-instance materials at the same indices.
        /// </summary>
        public UFBXList<UFBXMaterial> materials;

        /// <summary>
        /// Face groups for this mesh.
        /// </summary>
        public UFBXList<UFBXFaceGroup> faceGroups;

        /// <summary>
        /// Segments that use a given material.
        /// Defined even if the mesh doesn't have any materials.
        /// </summary>
        public UFBXList<UFBXMeshPart> materialParts;

        /// <summary>
        /// Segments for each face group.
        /// </summary>
        public UFBXList<UFBXMeshPart> faceGroupParts;

        /// <summary>
        /// Order of `material_parts` by first face that refers to it.
        /// Useful for compatibility with FBX SDK and various importers using it,
        /// as they use this material order by default.
        /// </summary>
        public UFBXList<uint> materialPartUsageOrder;

        // Skinned vertex positions, for efficiency the skinned positions are the
        // same as the static ones for non-skinned meshes and `skinned_is_local`
        // is set to true meaning you need to transform them manually using
        // `ufbx_transform_position(&node->geometry_to_world, skinned_pos)`!

        [MarshalAs(UnmanagedType.I1)]
        public bool skinnedIsLocal;
        public UFBXVertexVector3 skinnedPosition;
        public UFBXVertexVector3 skinnedNormal;

        // Deformers

        public UFBXList<UFBXSkinDeformer> skinDeformers;
        public UFBXList<UFBXBlendDeformer> blendDeformers;
        public UFBXList<UFBXCacheDeformer> cacheDeformers;
        public UFBXList<UFBXElement> allDeformers;

        //Subdivision
        public uint subdivisionPreviewLevels;
        public uint subdivisionRenderLevels;
        public UFBXSubdivisionDisplayMode subdivisionDisplayMode;
        public UFBXSubdivisionBoundary subdivisionBoundary;
        public UFBXSubdivisionBoundary subdivisionUVBoundary;

        /// <summary>
        /// The winding of the faces has been reversed.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool reverseWinding;

        /// <summary>
        /// Normals have been generated instead of evaluated.
        /// Either from missing normals (via `ufbx_load_opts.generate_missing_normals`), skinning,
        /// tessellation, or subdivision.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool generatedNormals;

        /// <summary>
        /// Subdivision (result)
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool subdivisionEvaluated;

        /// <summary>
        /// Subdivision (result)
        /// </summary>
        public UFBXSubdivisionResult* subdivisionResult;

        /// <summary>
        /// Tessellation (result)
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool fromTessellatedNurbs;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXLight
    {
        public UFBXElement element;

        /// <summary>
        /// Color and intensity of the light, usually you want to use `color * intensity`
        /// NOTE: `intensity` is 0.01x of the property `"Intensity"` as that matches
        /// matches values in DCC programs before exporting.
        /// </summary>
        public Vector3 color;
        /// <summary>
        /// Color and intensity of the light, usually you want to use `color * intensity`
        /// NOTE: `intensity` is 0.01x of the property `"Intensity"` as that matches
        /// matches values in DCC programs before exporting.
        /// </summary>
        public float intensity;

        /// <summary>
        /// Direction the light is aimed at in node's local space, usually -Y
        /// </summary>
        public Vector3 localDirection;

        public UFBXLightType type;
        public UFBXLightDecay decay;
        public UFBXLightAreaShape areaShape;
        public float innerAngle;
        public float outerAngle;

        [MarshalAs(UnmanagedType.I1)]
        public bool castLight;
        [MarshalAs(UnmanagedType.I1)]
        public bool castShadows;
    }

    /// <summary>
    /// Coordinate axes the scene is represented in.
    /// NOTE: `front` is the _opposite_ from forward!
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXCoordinateAxes
    {
        public UFBXCoordinateAxis right;
        public UFBXCoordinateAxis up;
        public UFBXCoordinateAxis front;
    }

    /// <summary>
    /// Camera attached to a `ufbx_node`
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXCamera
    {
        public UFBXElement element;

        /// <summary>
        /// Projection mode (perspective/orthographic).
        /// </summary>
        public UFBXProjectionMode projectionMode;

        /// <summary>
        /// If set to `true`, `resolution` represents actual pixel values, otherwise
        /// it's only useful for its aspect ratio.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool resolutionIsPixels;

        /// <summary>
        /// Render resolution, either in pixels or arbitrary units, depending on above
        /// </summary>
        public Vector2 resolution;

        /// <summary>
        /// Horizontal/vertical field of view in degrees
        /// Valid if `projection_mode == UFBX_PROJECTION_MODE_PERSPECTIVE`.
        /// </summary>
        public Vector2 fieldOfViewDeg;

        /// <summary>
        /// Component-wise `tan(field_of_view_deg)`, also represents the size of the
        /// proection frustum slice at distance of 1.
        /// Valid if `projection_mode == UFBX_PROJECTION_MODE_PERSPECTIVE`.
        /// </summary>
        public Vector2 fieldOfViewTan;

        /// <summary>
        /// Orthographic camera extents.
        /// Valid if `projection_mode == UFBX_PROJECTION_MODE_ORTHOGRAPHIC`.
        /// </summary>
        public float orthographicExtent;

        /// <summary>
        /// Orthographic camera size.
        /// Valid if `projection_mode == UFBX_PROJECTION_MODE_ORTHOGRAPHIC`.
        /// </summary>
        public Vector2 orthographicSize;

        /// <summary>
        /// Size of the projection plane at distance 1.
        /// Equal to `field_of_view_tan` if perspective, `orthographic_size` if orthographic.
        /// </summary>
        public Vector2 projectionPlane;

        /// <summary>
        /// Aspect ratio of the camera.
        /// </summary>
        public float aspectRatio;

        /// <summary>
        /// Near plane of the frustum in units from the camera.
        /// </summary>
        public float nearPlane;

        /// <summary>
        /// Far plane of the frustum in units from the camera.
        /// </summary>
        public float farPlane;

        /// <summary>
        /// Coordinate system that the projection uses.
        /// FBX saves cameras with +X forward and +Y up, but you can override this using
        /// `ufbx_load_opts.target_camera_axes` and it will be reflected here.
        /// </summary>
        public UFBXCoordinateAxes projectionAxes;

        // Advanced properties used to compute the above
        public UFBXAspectMode aspectMode;
        public UFBXApertureMode apertureMode;
        public UFBXGateFit gateFit;
        public UFBXApertureFormat apertureFormat;
        /// <summary>
        /// Focal length in millimeters
        /// </summary>
        public float focalLengthMM;
        /// <summary>
        /// Film size in inches
        /// </summary>
        public Vector2 filmSizeInch;
        /// <summary>
        /// Aperture/film gate size in inches
        /// </summary>
        public Vector2 apertureSizeInch;
        /// <summary>
        /// Anamoprhic stretch ratio
        /// </summary>
        public float squeezeRatio;
    }

    /// <summary>
    /// Bone attached to a `ufbx_node`, provides the logical length of the bone
    /// but most interesting information is directly in `ufbx_node`.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXBone
    {
        public UFBXElement element;

        /// <summary>
        /// Visual radius of the bone
        /// </summary>
        public float radius;

        /// <summary>
        /// Length of the bone relative to the distance between two nodes
        /// </summary>
        public float relativeLength;

        /// <summary>
        /// Is the bone a root bone
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool isRoot;
    }

    /// <summary>
    /// Empty/NULL/locator connected to a node, actual details in `ufbx_node`
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXEmpty
    {
        public UFBXElement element;
    }

    /// <summary>
    /// Segment of a `ufbx_line_curve`, indices refer to `ufbx_line_curve.point_indices[]`
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXLineSegment
    {
        public uint startIndex;
        public uint indexCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXLineCurve
    {
        public UFBXElement element;

        public Vector3 color;

        /// <summary>
        /// List of possible values the line passes through
        /// </summary>
        public UFBXList<Vector3> controlPoints;

        /// <summary>
        /// Indices to `control_points[]` the line goes through
        /// </summary>
        public UFBXList<uint> pointIndices;

        public UFBXList<UFBXLineSegment> segments;

        /// <summary>
        /// Tessellation (result)
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool fromTessellatedNURBS;
    }

    /// <summary>
    /// NURBS basis functions for an axis
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXNURBSBasis
    {
        /// <summary>
        /// Number of control points influencing a point on the curve/surface.
        /// Equal to the degree plus one.
        /// </summary>
        public uint order;

        /// <summary>
        /// Topology (periodicity) of the dimension.
        /// </summary>
        public UFBXNurbsTopology topology;

        /// <summary>
        /// Subdivision of the parameter range to control points.
        /// </summary>
        public UFBXList<float> knotVector;

        /// <summary>
        /// Range for the parameter value.
        /// </summary>
        public float tMin;
        /// <summary>
        /// Range for the parameter value.
        /// </summary>
        public float tMax;

        /// <summary>
        /// Parameter values of control points.
        /// </summary>
        public UFBXList<float> spans;

        /// <summary>
        /// `true` if this axis is two-dimensional.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool is2D;

        /// <summary>
        /// Number of control points that need to be copied to the end.
        /// This is just for convenience as it could be derived from `topology` and
        /// `order`. If for example `num_wrap_control_points == 3` you should repeat
        /// the first 3 control points after the end.
        /// HINT: You don't need to worry about this if you use ufbx functions
        /// like `ufbx_evaluate_nurbs_curve()` as they handle this internally.
        /// </summary>
        public ulong numWrapControlPoints;

        /// <summary>
        /// `true` if the parametrization is well defined.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool valid;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXNURBSCurve
    {
        public UFBXElement element;

        /// <summary>
        /// Basis in the U axis
        /// </summary>
        public UFBXNURBSBasis basis;

        /// <summary>
        /// Linear array of control points
        /// NOTE: The control points are _not_ homogeneous, meaning you have to multiply
        /// them by `w` before evaluating the surface.
        /// </summary>
        public UFBXList<Vector4> controlPoints;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXNURBSSurface
    {
        public UFBXElement element;

        /// <summary>
        /// Basis in the U/V axes
        /// </summary>
        public UFBXNURBSBasis basisU;
        /// <summary>
        /// Basis in the U/V axes
        /// </summary>
        public UFBXNURBSBasis basisV;

        /// <summary>
        /// Number of control points for the U/V axes
        /// </summary>
        public ulong controlPointsUCount;
        /// <summary>
        /// Number of control points for the U/V axes
        /// </summary>
        public ulong controlPointsVCount;

        /// <summary>
        /// 2D array of control points.
        /// Memory layout: `V * num_control_points_u + U`
        /// NOTE: The control points are _not_ homogeneous, meaning you have to multiply
        /// them by `w` before evaluating the surface.
        /// </summary>
        public UFBXList<Vector4> controlPoints;

        /// <summary>
        /// How many segments tessellate each span in `ufbx_nurbs_basis.spans`.
        /// </summary>
        public uint spanSubdivisionU;
        /// <summary>
        /// How many segments tessellate each span in `ufbx_nurbs_basis.spans`.
        /// </summary>
        public uint spanSubdivisionV;

        /// <summary>
        /// If `true` the resulting normals should be flipped when evaluated.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool flipNormals;

        /// <summary>
        /// Material for the whole surface.
        /// NOTE: May be `NULL`!
        /// </summary>
        public UFBXMaterial* material;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXNURBSTrimSurface
    {
        public UFBXElement element;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXNURBSTrimBoundary
    {
        public UFBXElement element;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXProceduralGeometry
    {
        public UFBXElement element;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXStereoCamera
    {
        public UFBXElement element;

        public UFBXCamera* left;
        public UFBXCamera* right;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXCameraSwitcher
    {
        public UFBXElement element;
    }

    /// <summary>
    /// Tracking marker for effectors
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXMarker
    {
        public UFBXElement element;

        /// <summary>
        /// Type of the marker
        /// </summary>
        public UFBXMarkerType type;
    }

    /// <summary>
    /// Single LOD level within an LOD group.
    /// Specifies properties of the Nth child of the _node_ containing the LOD group.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXLODLevel
    {
        /// <summary>
        /// Minimum distance to show this LOD level.
        /// NOTE: In world units by default, or in screen percentage if
        /// `ufbx_lod_group.relative_distances` is set.
        /// </summary>
        public float distance;

        /// <summary>
        /// LOD display mode.
        /// NOTE: Mostly for editing, you should probably ignore this
        /// unless making a modeling program.
        /// </summary>
        public UFBXLODDisplay display;
    }

    /// <summary>
    /// Group of LOD (Level of Detail) levels for an object.
    /// The actual LOD models are defined in the parent `ufbx_node.children`.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXLODGroup
    {
        public UFBXElement element;

        /// <summary>
        /// If set to `true`, `ufbx_lod_level.distance` represents a screen size percentage.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool relativeDistances;

        /// <summary>
        /// LOD levels matching in order to `ufbx_node.children`.
        /// </summary>
        public UFBXList<UFBXLODLevel> lodLevels;

        /// <summary>
        /// If set to `true` don't account for parent transform when computing the distance.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool ignoreParentTransform;

        /// <summary>
        /// If `use_distance_limit` is enabled hide the group if the distance is not between
        /// `distance_limit_min` and `distance_limit_max`.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool useDistanceLimit;

        /// <summary>
        /// If `use_distance_limit` is enabled hide the group if the distance is not between
        /// `distance_limit_min` and `distance_limit_max`.
        /// </summary>
        public float distanceLimitMin;

        /// <summary>
        /// If `use_distance_limit` is enabled hide the group if the distance is not between
        /// `distance_limit_min` and `distance_limit_max`.
        /// </summary>
        public float distanceLimitMax;
    }

    /// <summary>
    /// Skin weight information for a single mesh vertex
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXSkinVertex
    {
        // Each vertex is influenced by weights from `ufbx_skin_deformer.weights[]`
        // The weights are sorted by decreasing weight so you can take the first N
        // weights to get a cheaper approximation of the vertex.
        // NOTE: The weights are not guaranteed to be normalized!

        /// <summary>
        /// Index to start from in the `weights[]` array
        /// </summary>
        public uint startWeight;

        /// <summary>
        /// Number of weights influencing the vertex
        /// </summary>
        public uint weightCount;

        /// <summary>
        /// Blend weight between Linear Blend Skinning (0.0) and Dual Quaternion (1.0).
        /// Should be used if `skinning_method == UFBX_SKINNING_METHOD_BLENDED_DQ_LINEAR`
        /// </summary>
        public float dqWeight;
    }

    /// <summary>
    /// Single per-vertex per-cluster weight, see `ufbx_skin_vertex`
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXSkinWeight
    {
        /// <summary>
        /// Index into `ufbx_skin_deformer.clusters[]`
        /// </summary>
        public uint clusterIndex;

        /// <summary>
        /// Amount this bone influence the vertex
        /// </summary>
        public float weight;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXSkinDeformer
    {
        public UFBXElement element;

        public UFBXSkinningMethod skinningMethod;

        /// <summary>
        /// Clusters (bones) in the skin
        /// </summary>
        public UFBXList<UFBXSkinCluster> clusters;
        /// <summary>
        /// Per-vertex weight information
        /// </summary>
        public UFBXList<UFBXSkinVertex> vertices;
        /// <summary>
        /// Per-vertex weight information
        /// </summary>
        public UFBXList<UFBXSkinWeight> weights;

        /// <summary>
        /// Largest amount of weights a single vertex can have
        /// </summary>
        public ulong maxWeightsPerVertex;

        /// <summary>
        /// Blend weights between Linear Blend Skinning (0.0) and Dual Quaternion (1.0).
        /// HINT: You probably want to use `vertices` and `ufbx_skin_vertex.dq_weight` instead!
        /// NOTE: These may be out-of-bounds for a given mesh, `vertices` is always safe.
        /// </summary>
        public ulong dqWeightCount;
        /// <summary>
        /// Blend weights between Linear Blend Skinning (0.0) and Dual Quaternion (1.0).
        /// HINT: You probably want to use `vertices` and `ufbx_skin_vertex.dq_weight` instead!
        /// NOTE: These may be out-of-bounds for a given mesh, `vertices` is always safe.
        /// </summary>
        public UFBXList<uint> dqVertices;
        /// <summary>
        /// Blend weights between Linear Blend Skinning (0.0) and Dual Quaternion (1.0).
        /// HINT: You probably want to use `vertices` and `ufbx_skin_vertex.dq_weight` instead!
        /// NOTE: These may be out-of-bounds for a given mesh, `vertices` is always safe.
        /// </summary>
        public UFBXList<float> dqWeights;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXSkinCluster
    {
        public UFBXElement element;

        /// <summary>
        /// The bone node the cluster is attached to
        /// NOTE: Always valid if found from `ufbx_skin_deformer.clusters[]` unless
        /// `ufbx_load_opts.connect_broken_elements` is `true`.
        /// </summary>
        public UFBXNode* boneNode;

        /// <summary>
        /// Binding matrix from local mesh vertices to the bone
        /// </summary>
        public UFBXMatrix geometryToNode;

        /// <summary>
        /// Binding matrix from local mesh _node_ to the bone.
        /// NOTE: Prefer `geometry_to_bone` in most use cases!
        /// </summary>
        public UFBXMatrix meshNodeToBone;

        /// <summary>
        /// Matrix that specifies the rest/bind pose transform of the node,
        /// not generally needed for skinning, use `geometry_to_bone` instead.
        /// </summary>
        public UFBXMatrix bindToWorld;

        /// <summary>
        /// Precomputed matrix/transform that accounts for the current bone transform
        /// ie. `ufbx_matrix_mul(&amp;cluster->bone->node_to_world, &amp;cluster->geometry_to_bone)`
        /// </summary>
        public UFBXMatrix geometryToWorld;

        public UFBXTransform geometryToWorldTransform;

        // Raw weights indexed by each _vertex_ of a mesh (not index!)
        // HINT: It may be simpler to use `ufbx_skin_deformer.vertices[]/weights[]` instead!
        // NOTE: These may be out-of-bounds for a given mesh, `ufbx_skin_deformer.vertices` is always safe.

        /// <summary>
        /// Number of vertices in the cluster
        /// </summary>
        public ulong weightCount;

        /// <summary>
        /// Vertex indices in `ufbx_mesh.vertices[]`
        /// </summary>
        public UFBXList<uint> vertices;

        /// <summary>
        /// Per-vertex weight values
        /// </summary>
        public UFBXList<float> weights;
    }

    /// <summary>
    /// Blend shape deformer can contain multiple channels (think of sliders between morphs)
    /// that may optionally have in-between keyframes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXBlendDeformer
    {
        public UFBXElement element;

        /// <summary>
        /// Independent morph targets of the deformer.
        /// </summary>
        public UFBXList<UFBXBlendChannel> channels;
    }

    /// <summary>
    /// Blend shape associated with a target weight in a series of morphs
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXBlendKeyframe
    {
        /// <summary>
        /// The target blend shape offsets.
        /// </summary>
        public UFBXBlendShape* shape;

        /// <summary>
        /// Weight value at which to apply the keyframe at full strength
        /// </summary>
        public float targetWeight;

        /// <summary>
        /// The weight the shape should be currently applied with
        /// </summary>
        public float effectiveWeight;
    }

    /// <summary>
    /// Blend channel consists of multiple morph-key targets that are interpolated.
    /// In simple cases there will be only one keyframe that is the target shape.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXBlendChannel
    {
        public UFBXElement element;

        /// <summary>
        /// Current weight of the channel
        /// </summary>
        public float weight;

        /// <summary>
        /// Key morph targets to blend between depending on `weight`
        /// In usual cases there's only one target per channel
        /// </summary>
        public UFBXList<UFBXBlendKeyframe> keyframes;

        /// <summary>
        /// Final blend shape ignoring any intermediate blend shapes.
        /// </summary>
        public UFBXBlendShape* targetShape;
    }

    /// <summary>
    /// Blend shape target containing the actual vertex offsets
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXBlendShape
    {
        public UFBXElement element;

        // Vertex offsets to apply over the base mesh
        // NOTE: The `offset_vertices` may be out-of-bounds for a given mesh!

        /// <summary>
        /// Number of vertex offsets in the following arrays
        /// </summary>
        public ulong offsetCount;

        /// <summary>
        /// Indices to `ufbx_mesh.vertices[]`
        /// </summary>
        public UFBXList<uint> offsetVertices;

        /// <summary>
        /// Always specified per-vertex offsets
        /// </summary>
        public UFBXList<Vector3> positionOffsets;

        /// <summary>
        /// Empty if not specified
        /// </summary>
        public UFBXList<Vector3> normalOffsets;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXCacheFrame
    {
        /// <summary>
        /// Name of the channel this frame belongs to.
        /// </summary>
        public UFBXString channel;

        /// <summary>
        /// Time of this frame in seconds.
        /// </summary>
        public double time;

        /// <summary>
        /// Name of the file containing the data.
        /// The specified file may contain multiple frames, use `data_offset` etc. to
        /// read at the right position.
        /// </summary>
        public UFBXString fileName;

        /// <summary>
        /// Format of the wrapper file.
        /// </summary>
        public UFBXCacheFileFormat fileFormat;

        /// <summary>
        /// Axis to mirror the read data by.
        /// </summary>
        public UFBXMirrorAxis mirrorAxis;

        /// <summary>
        /// Factor to scale the geometry by.
        /// </summary>
        public float scaleFactor;

        /// <summary>
        /// Format of the data in the file
        /// </summary>
        public UFBXCacheDataFormat dataFormat;

        /// <summary>
        /// Binary encoding of the data
        /// </summary>
        public UFBXCacheDataEncoding dataEncoding;

        /// <summary>
        /// Byte offset into the file
        /// </summary>
        public ulong dataOffset;

        /// <summary>
        /// Number of data elements
        /// </summary>
        public uint dataCount;

        /// <summary>
        /// Size of a single data element in bytes
        /// </summary>
        public uint dataElementBytes;

        /// <summary>
        /// Size of the whole data blob in bytes
        /// </summary>
        public ulong dataTotalBytes;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXCacheChannel
    {
        /// <summary>
        /// Name of the geometry cache channel.
        /// </summary>
        public UFBXString name;

        /// <summary>
        /// What does the data in this channel represent.
        /// </summary>
        public UFBXCacheInterpretation interpretation;

        /// <summary>
        /// Source name for `interpretation`, especially useful if `interpretation` is
        /// `UFBX_CACHE_INTERPRETATION_UNKNOWN`.
        /// </summary>
        public UFBXString interpretationName;

        /// <summary>
        /// List of frames belonging to this channel.
        /// Sorted by time (<see cref="UFBXCacheFrame.time"/>).
        /// </summary>
        public UFBXList<UFBXCacheFrame> frames;

        /// <summary>
        /// Axis to mirror the frames by.
        /// </summary>
        public UFBXMirrorAxis mirrorAxis;

        /// <summary>
        /// Factor to scale the geometry by.
        /// </summary>
        public float scaleFactor;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXGeometryCache
    {
        public UFBXString rootFileName;
        public UFBXList<UFBXCacheChannel> channels;
        public UFBXList<UFBXCacheFrame> frames;
        public UFBXStringList extraInfo;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXCacheDeformer
    {
        public UFBXElement element;

        public UFBXString channel;

        public UFBXCacheFile* file;

        /// <summary>
        /// Only valid if `ufbx_load_opts.load_external_files` is set!
        /// </summary>
        public UFBXGeometryCache* externalCache;

        /// <summary>
        /// Only valid if `ufbx_load_opts.load_external_files` is set!
        /// </summary>
        public UFBXCacheChannel* externalChannel;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXCacheFile
    {
        public UFBXElement element;

        /// <summary>
        /// Filename relative to the currently loaded file.
        /// HINT: If using functions other than `ufbx_load_file()`, you can provide
        /// `ufbx_load_opts.filename/raw_filename` to let ufbx resolve this.
        /// </summary>
        public UFBXString fileName;

        /// <summary>
        /// Absolute filename specified in the file.
        /// </summary>
        public UFBXString absoluteFileName;

        /// <summary>
        /// Relative filename specified in the file.
        /// NOTE: May be absolute if the file is saved in a different drive.
        /// </summary>
        public UFBXString relativeFileName;

        /// <summary>
        /// Filename relative to the loaded file, non-UTF-8 encoded.
        /// HINT: If using functions other than `ufbx_load_file()`, you can provide
        /// `ufbx_load_opts.filename/raw_filename` to let ufbx resolve this.
        /// </summary>
        public UFBXBlob rawFileName;

        /// <summary>
        /// Absolute filename specified in the file, non-UTF-8 encoded.
        /// </summary>
        public UFBXBlob rawAbsoluteFileName;

        /// <summary>
        /// Relative filename specified in the file, non-UTF-8 encoded.
        /// NOTE: May be absolute if the file is saved in a different drive.
        /// </summary>
        public UFBXBlob rawRelativeFileName;

        public UFBXCacheFileFormat format;

        /// <summary>
        /// Only valid if `ufbx_load_opts.load_external_files` is set!
        /// </summary>
        public UFBXGeometryCache* externalCache;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXMaterialMap
    {
        public Vector4 vector44Value;
        public long intValue;

        /// <summary>
        /// Texture if connected, otherwise `NULL`.
        /// May be valid but "disabled" (application specific) if `texture_enabled == false`.
        /// </summary>
        public UFBXTexture* texture;

        /// <summary>
        /// `true` if the file has specified any of the values above.
        /// NOTE: The value may be set to a non-zero default even if `has_value == false`,
        /// for example missing factors are set to `1.0` if a color is defined.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool hasValue;

        /// <summary>
        /// Controls whether shading should use `texture`.
        /// NOTE: Some shading models allow this to be `true` even if `texture == NULL`.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool textureEnabled;

        /// <summary>
        /// Set to `true` if this feature should be disabled (specific to shader type).
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool featureDisabled;

        /// <summary>
        /// Number of components in the value from 1 to 4 if defined, 0 if not.
        /// </summary>
        public byte valueComponents;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXMaterialFeatureInfo
    {
        /// <summary>
        /// Whether the material model uses this feature or not.
        /// NOTE: The feature can be enabled but still not used if eg. the corresponding factor is at zero!
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool enabled;

        /// <summary>
        /// Explicitly enabled/disabled by the material.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool isExplicit;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXMaterialTexture
    {
        /// <summary>
        /// Name of the property in `ufbx_material.props`
        /// </summary>
        public UFBXString materialProp;

        /// <summary>
        /// Shader-specific property mapping name
        /// </summary>
        public UFBXString shaderProp;

        /// <summary>
        /// Texture attached to the property.
        /// </summary>
        public UFBXMaterialTexture* texture;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXMaterialFBXMaps
    {
        public UFBXMaterialMap diffuseFactor;
        public UFBXMaterialMap diffuseColor;
        public UFBXMaterialMap specularFactor;
        public UFBXMaterialMap specularColor;
        public UFBXMaterialMap specularExponent;
        public UFBXMaterialMap reflectionFactor;
        public UFBXMaterialMap reflectionColor;
        public UFBXMaterialMap transparencyFactor;
        public UFBXMaterialMap transparencyColor;
        public UFBXMaterialMap emissionFactor;
        public UFBXMaterialMap emissionColor;
        public UFBXMaterialMap ambientFactor;
        public UFBXMaterialMap ambientColor;
        public UFBXMaterialMap normalMap;
        public UFBXMaterialMap bump;
        public UFBXMaterialMap bumpFactor;
        public UFBXMaterialMap displacementFactor;
        public UFBXMaterialMap displacement;
        public UFBXMaterialMap vectorDisplacementFactor;
        public UFBXMaterialMap vectorDisplacement;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXMaterialPBRMaps
    {
        public UFBXMaterialMap baseFactor;
        public UFBXMaterialMap baseColor;
        public UFBXMaterialMap roughness;
        public UFBXMaterialMap metalness;
        public UFBXMaterialMap diffuseRoughness;
        public UFBXMaterialMap specularFactor;
        public UFBXMaterialMap specularColor;
        public UFBXMaterialMap specularIOR;
        public UFBXMaterialMap specularAnisotropy;
        public UFBXMaterialMap specularRotation;
        public UFBXMaterialMap transmissionFactor;
        public UFBXMaterialMap transmissionColor;
        public UFBXMaterialMap transmissionDepth;
        public UFBXMaterialMap transmissionScatter;
        public UFBXMaterialMap transmissionScatterAnisotropy;
        public UFBXMaterialMap transmissionDispersion;
        public UFBXMaterialMap transmissionRoughness;
        public UFBXMaterialMap transmissionExtraRoughness;
        public UFBXMaterialMap transmissionPriority;
        public UFBXMaterialMap transmissionEnableInAOV;
        public UFBXMaterialMap subsurfaceFactor;
        public UFBXMaterialMap subsurfaceColor;
        public UFBXMaterialMap subsurfaceRadius;
        public UFBXMaterialMap subsurfaceScale;
        public UFBXMaterialMap subsurfaceAnisotropy;
        public UFBXMaterialMap subsurfaceTGintColor;
        public UFBXMaterialMap subsurfaceType;
        public UFBXMaterialMap sheenFactor;
        public UFBXMaterialMap sheenColor;
        public UFBXMaterialMap sheenRoughness;
        public UFBXMaterialMap coatFactor;
        public UFBXMaterialMap coatColor;
        public UFBXMaterialMap coatRoughness;
        public UFBXMaterialMap coatIOR;
        public UFBXMaterialMap coatAnisotropy;
        public UFBXMaterialMap coatRotation;
        public UFBXMaterialMap coatNormal;
        public UFBXMaterialMap coatAffectBaseColor;
        public UFBXMaterialMap coatAffectBaseRoughness;
        public UFBXMaterialMap thinFilmFactor;
        public UFBXMaterialMap thinFilmThickness;
        public UFBXMaterialMap thinFilmIOR;
        public UFBXMaterialMap emissionFactor;
        public UFBXMaterialMap emissionColor;
        public UFBXMaterialMap opacity;
        public UFBXMaterialMap indirectDiffuse;
        public UFBXMaterialMap indirectSpecular;
        public UFBXMaterialMap normalMap;
        public UFBXMaterialMap tangentMap;
        public UFBXMaterialMap displacementMap;
        public UFBXMaterialMap matteFactor;
        public UFBXMaterialMap matteColor;
        public UFBXMaterialMap ambientOcclusion;
        public UFBXMaterialMap glossiness;
        public UFBXMaterialMap coatGlossiness;
        public UFBXMaterialMap transmissionGlossiness;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXMaterialFeatures
    {
        public UFBXMaterialFeatureInfo pbr;
        public UFBXMaterialFeatureInfo metalness;
        public UFBXMaterialFeatureInfo diffuse;
        public UFBXMaterialFeatureInfo specular;
        public UFBXMaterialFeatureInfo emission;
        public UFBXMaterialFeatureInfo transmission;
        public UFBXMaterialFeatureInfo coat;
        public UFBXMaterialFeatureInfo sheen;
        public UFBXMaterialFeatureInfo opacity;
        public UFBXMaterialFeatureInfo ambientOcclusion;
        public UFBXMaterialFeatureInfo matte;
        public UFBXMaterialFeatureInfo unlit;
        public UFBXMaterialFeatureInfo ior;
        public UFBXMaterialFeatureInfo diffuseRoughness;
        public UFBXMaterialFeatureInfo transmissionRoughness;
        public UFBXMaterialFeatureInfo thinWalled;
        public UFBXMaterialFeatureInfo caustics;
        public UFBXMaterialFeatureInfo exitToBackground;
        public UFBXMaterialFeatureInfo internalReflections;
        public UFBXMaterialFeatureInfo doubleSided;
        public UFBXMaterialFeatureInfo roughnessAsGlossiness;
        public UFBXMaterialFeatureInfo coatRoughnessAsGlossiness;
        public UFBXMaterialFeatureInfo transmissionRoughnessAsGlossiness;
    }

    /// <summary>
    /// Surface material properties such as color, roughness, etc. Each property may
    /// be optionally bound to an `ufbx_texture`.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXMaterial
    {
        public UFBXElement element;

        /// <summary>
        /// FBX builtin properties
        /// NOTE: These may be empty if the material is using a custom shader
        /// </summary>
        public UFBXMaterialFBXMaps fbx;

        /// <summary>
        /// PBR material properties, defined for all shading models but may be
        /// somewhat approximate if `shader == NULL`.
        /// </summary>
        public UFBXMaterialPBRMaps pbr;

        /// <summary>
        /// Material features, primarily applies to `pbr`.
        /// </summary>
        public UFBXMaterialFeatures features;

        /// <summary>
        /// Shading information. Always defined.
        /// </summary>
        public UFBXShaderType shaderType;

        /// <summary>
        /// Optional extended shader information
        /// </summary>
        public UFBXShader* shader;

        /// <summary>
        /// Often one of `{ "lambert", "phong", "unknown" }`
        /// </summary>
        public UFBXString shadingModelName;

        /// <summary>
        /// Prefix before shader property names with trailing `|`.
        /// For example `"3dsMax|Parameters|"` where properties would have names like
        /// `"3dsMax|Parameters|base_color"`. You can ignore this if you use the built-in
        /// `ufbx_material_fbx_maps fbx` and `ufbx_material_pbr_maps pbr` structures.
        /// </summary>
        public UFBXString shaderPropPrefix;

        /// <summary>
        /// All textures attached to the material, if you want specific maps if might be
        /// more convenient to use eg. `fbx.diffuse_color.texture` or `pbr.base_color.texture`
        /// Sorted by `material_prop`
        /// </summary>
        public UFBXList<UFBXMaterialTexture> textures;
    }

    /// <summary>
    /// Single layer in a layered texture
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXTextureLayer
    {
        /// <summary>
        /// The inner texture to evaluate, never `NULL`
        /// </summary>
        public UFBXTexture* texture;

        /// <summary>
        /// Equation to combine the layer to the background
        /// </summary>
        public UFBXBlendMode blendMode;

        /// <summary>
        /// Blend weight of this layer
        /// </summary>
        public float alpha;
    }

    /// <summary>
    /// Input to a shader texture, see `ufbx_shader_texture`.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXShaderTextureInput
    {
        /// <summary>
        /// Name of the input.
        /// </summary>
        public UFBXString name;

        public Vector4 vector4Value;
        public long intValue;
        public UFBXString stringValue;
        public UFBXBlob blobValue;

        /// <summary>
        /// Texture connected to this input.
        /// </summary>
        public UFBXTexture* texture;

        /// <summary>
        /// Index of the output to use if `texture` is a multi-output shader node.
        /// </summary>
        public long textureOutputIndex;

        /// <summary>
        /// Controls whether shading should use `texture`.
        /// NOTE: Some shading models allow this to be `true` even if `texture == NULL`.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool textureEnabled;

        /// <summary>
        /// Property representing this input.
        /// </summary>
        public UFBXProp* prop;

        /// <summary>
        /// Property representing `texture`.
        /// </summary>
        public UFBXProp* textureProp;

        /// <summary>
        /// Property representing `texture_enabled`.
        /// </summary>
        public UFBXProp* textureEnabledProp;
    }

    /// <summary>
    /// Texture that emulates a shader graph node.
    /// 3ds Max exports some materials as node graphs serialized to textures.
    /// ufbx can parse a small subset of these, as normal maps are often hidden behind
    /// some kind of bump node.
    /// NOTE: These encode a lot of details of 3ds Max internals, not recommended for direct use.
    /// HINT: `ufbx_texture.file_textures[]` contains a list of "real" textures that are connected
    /// to the `ufbx_texture` that is pretending to be a shader node.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXShaderTexture
    {
        /// <summary>
        /// Type of this shader node.
        /// </summary>
        public UFBXShaderTextureType type;

        /// <summary>
        /// Name of the shader to use.
        /// </summary>
        public UFBXString shaderName;

        /// <summary>
        /// 64-bit opaque identifier for the shader type.
        /// </summary>
        public ulong shaderTypeID;

        /// <summary>
        /// Input values/textures (possibly further shader textures) to the shader.
        /// Sorted by `ufbx_shader_texture_input.name`.
        /// </summary>
        public UFBXList<UFBXShaderTextureInput> inputs;
        /// <summary>
        /// Shader source code if found.
        /// </summary>
        public UFBXString shaderSource;
        /// <summary>
        /// Shader source code if found.
        /// </summary>
        public UFBXBlob rawShaderSource;

        /// <summary>
        /// Representative texture for this shader.
        /// Only specified if `main_texture.outputs[main_texture_output_index]` is semantically
        /// equivalent to this texture.
        /// </summary>
        public UFBXTexture* mainTexture;

        /// <summary>
        /// Output index of `main_texture` if it is a multi-output shader.
        /// </summary>
        public long mainTextureOutputIndex;

        /// <summary>
        /// Prefix for properties related to this shader in `ufbx_texture`.
        /// NOTE: Contains the trailing '|' if not empty.
        /// </summary>
        public UFBXString propPrefix;
    }

    /// <summary>
    /// Unique texture within the file.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXTextureFile
    {
        /// <summary>
        /// Index in `ufbx_scene.texture_files[]`.
        /// </summary>
        public uint index;

        /// <summary>
        /// Filename relative to the currently loaded file.
        /// HINT: If using functions other than `ufbx_load_file()`, you can provide
        /// `ufbx_load_opts.filename/raw_filename` to let ufbx resolve this.
        /// </summary>
        public UFBXString fileName;

        /// <summary>
        /// Absolute filename specified in the file.
        /// </summary>
        public UFBXString absoluteFileName;
        /// <summary>
        /// Relative filename specified in the file.
        /// NOTE: May be absolute if the file is saved in a different drive.
        /// </summary>
        public UFBXString relativeFileName;

        /// <summary>
        /// Filename relative to the loaded file, non-UTF-8 encoded.
        /// HINT: If using functions other than `ufbx_load_file()`, you can provide
        /// `ufbx_load_opts.filename/raw_filename` to let ufbx resolve this.
        /// </summary>
        public UFBXBlob rawFileName;

        /// <summary>
        /// Absolute filename specified in the file, non-UTF-8 encoded.
        /// </summary>
        public UFBXBlob rawAbsoluteFileName;

        /// <summary>
        /// Relative filename specified in the file, non-UTF-8 encoded.
        /// NOTE: May be absolute if the file is saved in a different drive.
        /// </summary>
        public UFBXBlob rawRelativeFileName;

        /// <summary>
        /// Optional embedded content blob, eg. raw .png format data
        /// </summary>
        public UFBXBlob content;
    }

    /// <summary>
    /// Texture that controls material appearance
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXTexture
    {
        public UFBXElement element;

        /// <summary>
        /// Texture type (file / layered / procedural / shader)
        /// </summary>
        public UFBXTextureType type;

        /// <summary>
        /// Filename relative to the currently loaded file.
        /// HINT: If using functions other than `ufbx_load_file()`, you can provide
        /// `ufbx_load_opts.filename/raw_filename` to let ufbx resolve this.
        /// </summary>
        public UFBXString fileName;

        /// <summary>
        /// Absolute filename specified in the file.
        /// </summary>
        public UFBXString absoluteFileName;

        /// <summary>
        /// Relative filename specified in the file.
        /// NOTE: May be absolute if the file is saved in a different drive.
        /// </summary>
        public UFBXString relativeFileName;

        /// <summary>
        /// Filename relative to the loaded file, non-UTF-8 encoded.
        /// HINT: If using functions other than `ufbx_load_file()`, you can provide
        /// `ufbx_load_opts.filename/raw_filename` to let ufbx resolve this.
        /// </summary>
        public UFBXBlob rawFileName;

        /// <summary>
        /// Absolute filename specified in the file, non-UTF-8 encoded.
        /// </summary>
        public UFBXBlob rawAbsoluteFileName;

        /// <summary>
        /// Relative filename specified in the file, non-UTF-8 encoded.
        /// NOTE: May be absolute if the file is saved in a different drive.
        /// </summary>
        public UFBXBlob rawRelativeFileName;

        /// <summary>
        /// FILE: Optional embedded content blob, eg. raw .png format data
        /// </summary>
        public UFBXBlob content;

        /// <summary>
        /// FILE: Optional video texture
        /// </summary>
        public UFBXVideo* video;

        /// <summary>
        /// FILE: Index into `ufbx_scene.texture_files[]` or `UFBX_NO_INDEX`.
        /// </summary>
        public uint fileIndex;

        /// <summary>
        /// FILE: True if `file_index` has a valid value.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool hasFile;

        /// <summary>
        /// LAYERED: Inner texture layers, ordered from _bottom_ to _top_
        /// </summary>
        public UFBXList<UFBXTextureLayer> layers;

        /// <summary>
        /// SHADER: Shader information
        /// NOTE: May be specified even if `type == UFBX_TEXTURE_FILE` if `ufbx_load_opts.disable_quirks`
        /// is _not_ specified. Some known shaders that represent files are interpreted as `UFBX_TEXTURE_FILE`.
        /// </summary>
        public UFBXShaderTexture* shader;

        /// <summary>
        /// List of file textures representing this texture.
        /// Defined even if `type == UFBX_TEXTURE_FILE` in which case the array contains only itself.
        /// </summary>
        public UFBXList<UFBXTexture> fileTextures;

        /// <summary>
        /// Name of the UV set to use
        /// </summary>
        public UFBXString uvSet;

        /// <summary>
        /// Wrapping mode
        /// </summary>
        public UFBXWrapMode wrapU;

        /// <summary>
        /// Wrapping mode
        /// </summary>
        public UFBXWrapMode wrapV;

        /// <summary>
        /// Has a non-identity `transform` and derived matrices.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool hasUVTransform;

        /// <summary>
        /// Texture transformation in UV space
        /// </summary>
        public UFBXTransform uvTransform;

        /// <summary>
        /// Matrix representation of `transform`
        /// </summary>
        public UFBXMatrix textureToUV;

        /// <summary>
        /// UV coordinate to normalized texture coordinate matrix
        /// </summary>
        public UFBXMatrix uvToTexture;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXVideo
    {
        public UFBXElement element;

        /// <summary>
        /// Filename relative to the currently loaded file.
        /// HINT: If using functions other than `ufbx_load_file()`, you can provide
        /// `ufbx_load_opts.filename/raw_filename` to let ufbx resolve this.
        /// </summary>
        public UFBXString fileName;

        /// <summary>
        /// Absolute filename specified in the file.
        /// </summary>
        public UFBXString absoluteFileName;

        /// <summary>
        /// Relative filename specified in the file.
        /// NOTE: May be absolute if the file is saved in a different drive.
        /// </summary>
        public UFBXString relativeFileName;

        /// <summary>
        /// Filename relative to the loaded file, non-UTF-8 encoded.
        /// HINT: If using functions other than `ufbx_load_file()`, you can provide
        /// `ufbx_load_opts.filename/raw_filename` to let ufbx resolve this.
        /// </summary>
        public UFBXBlob rawFileName;

        /// <summary>
        /// Absolute filename specified in the file, non-UTF-8 encoded.
        /// </summary>
        public UFBXBlob rawAbsoluteFileName;

        /// <summary>
        /// Relative filename specified in the file, non-UTF-8 encoded.
        /// NOTE: May be absolute if the file is saved in a different drive.
        /// </summary>
        public UFBXBlob rawRelativeFileName;

        /// <summary>
        /// Optional embedded content blob
        /// </summary>
        public UFBXBlob content;
    }

    /// <summary>
    /// Shader specifies a shading model and contains `ufbx_shader_binding` elements
    /// that define how to interpret FBX properties in the shader.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXShader
    {
        public UFBXElement element;

        /// <summary>
        /// Known shading model
        /// </summary>
        public UFBXShaderType type;

        /// <summary>
        /// Bindings from FBX properties to the shader
        /// HINT: `ufbx_find_shader_prop()` translates shader properties to FBX properties
        /// </summary>
        public UFBXList<UFBXShaderBinding> bindings;
    }

    /// <summary>
    /// Binding from a material property to shader implementation
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXShaderPropBinding
    {
        /// <summary>
        /// Property name used by the shader implementation
        /// </summary>
        public UFBXString shaderProp;

        /// <summary>
        /// Property name inside `ufbx_material.props`
        /// </summary>
        public UFBXString materialProp;
    }

    /// <summary>
    /// Shader binding table
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXShaderBinding
    {
        public UFBXElement element;

        /// <summary>
        /// Sorted by `shader_prop`
        /// </summary>
        public UFBXList<UFBXShaderPropBinding> propBindings;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXPropOverride
    {
        public uint elementID;
        public uint internalKey;
        public UFBXString propName;
        public Vector4 value;
        public UFBXString stringValue;
        public long intValue;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXTransformOverride
    {
        public uint nodeID;
        public UFBXTransform transform;
    }

    /// <summary>
    /// Animation descriptor used for evaluating animation.
    /// Usually obtained from `ufbx_scene` via either global animation `ufbx_scene.anim`,
    /// per-stack animation `ufbx_anim_stack.anim` or per-layer animation `ufbx_anim_layer.anim`.
    ///
    /// For advanced usage you can use `ufbx_create_anim()` to create animation descriptors
    /// with custom layers, property overrides, special flags, etc.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXAnim
    {
        /// <summary>
        /// Time begin/end for the animation, both may be zero if absent.
        /// </summary>
        public double startTime;

        /// <summary>
        /// Time begin/end for the animation, both may be zero if absent.
        /// </summary>
        public double endTime;

        /// <summary>
        /// List of layers in the animation.
        /// </summary>
        public UFBXList<UFBXAnimLayer> layers;

        /// <summary>
        /// Optional overrides for weights for each layer in `layers[]`.
        /// </summary>
        public UFBXList<float> overrideLayerWeights;

        /// <summary>
        /// Sorted by `element_id, prop_name`
        /// </summary>
        public UFBXList<UFBXPropOverride> propOverrides;

        /// <summary>
        /// Sorted by `node_id`
        /// </summary>
        public UFBXList<UFBXTransformOverride> transformOverrides;

        /// <summary>
        /// Evaluate connected properties as if they would not be connected.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool ignoreConnections;

        /// <summary>
        /// Custom `ufbx_anim` created by `ufbx_create_anim()`.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool custom;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXAnimStack
    {
        public UFBXElement element;

        public double startTime;
        public double endTime;

        public UFBXList<UFBXAnimLayer> layers;
        public UFBXAnim* anim;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXAnimProp
    {
        public UFBXElement element;

        public uint internalKey;

        public UFBXString propName;

        public UFBXAnimValue* animValue;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXAnimLayer
    {
        public UFBXElement element;

        public float weight;
        [MarshalAs(UnmanagedType.I1)]
        public bool weightIsAnimated;
        [MarshalAs(UnmanagedType.I1)]
        public bool blended;
        [MarshalAs(UnmanagedType.I1)]
        public bool additive;
        [MarshalAs(UnmanagedType.I1)]
        public bool composeRotation;
        [MarshalAs(UnmanagedType.I1)]
        public bool composeScale;

        public UFBXList<UFBXAnimValue> animValues;

        /// <summary>
        /// Sorted by `element,prop_name`
        /// </summary>
        public UFBXList<UFBXAnimProp> animProps;

        public UFBXAnim* anim;

        public uint minElementID;
        public uint maxElementID;
        public uint elementIDBitmask0;
        public uint elementIDBitmask1;
        public uint elementIDBitmask2;
        public uint elementIDBitmask3;

        public readonly uint[] ElementIDBitmask => [
            elementIDBitmask0, elementIDBitmask1,
            elementIDBitmask2, elementIDBitmask3
        ];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXAnimValue
    {
        public UFBXElement element;

        public Vector3 defaultValue;

        public UFBXAnimCurve* curve0;
        public UFBXAnimCurve* curve1;
        public UFBXAnimCurve* curve2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXExtrapolation
    {
        public UFBXExtrapolationMode mode;

        /// <summary>
        /// Count used for repeating modes.
        /// Negative values mean infinite repetition.
        /// </summary>
        public int repeatCount;
    }

    /// <summary>
    /// Tangent vector at a keyframe, may be split into left/right
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXTangent
    {
        /// <summary>
        /// Derivative in the time axis
        /// </summary>
        public float dx;

        /// <summary>
        /// Derivative in the (curve specific) value axis
        /// </summary>
        public float dy;
    }

    /// <summary>
    /// Single real `value` at a specified `time`, interpolation between two keyframes
    /// is determined by the `interpolation` field of the _previous_ key.
    /// If `interpolation == UFBX_INTERPOLATION_CUBIC` the span is evaluated as a
    /// cubic bezier curve through the following points:
    ///
    ///   (prev->time, prev->value)
    ///   (prev->time + prev->right.dx, prev->value + prev->right.dy)
    ///   (next->time - next->left.dx, next->value - next->left.dy)
    ///   (next->time, next->value)
    ///
    /// HINT: You can use `ufbx_evaluate_curve(ufbx_anim_curve *curve, double time)`
    /// rather than trying to manually handle all the interpolation modes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXKeyframe
    {
        public double time;
        public float value;
        public UFBXInterpolation interpolation;
        public UFBXTangent left;
        public UFBXTangent right;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXAnimCurve
    {
        public UFBXElement element;

        /// <summary>
        /// List of keyframes that define the curve.
        /// </summary>
        public UFBXList<UFBXKeyframe> keyframes;

        /// <summary>
        /// Extrapolation before the curve.
        /// </summary>
        public UFBXExtrapolation preExtrapolation;

        /// <summary>
        /// Extrapolation after the curve.
        /// </summary>
        public UFBXExtrapolation postExtrapolation;

        /// <summary>
        /// Value range for all the keyframes.
        /// </summary>
        public float minValue;

        /// <summary>
        /// Value range for all the keyframes.
        /// </summary>
        public float maxValue;

        /// <summary>
        /// Time range for all the keyframes.
        /// </summary>
        public double minTime;

        /// <summary>
        /// Time range for all the keyframes.
        /// </summary>
        public double maxTime;
    }

    /// <summary>
    /// Collection of nodes to hide/freeze
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXDisplayLayer
    {
        public UFBXElement element;

        /// <summary>
        /// Nodes included in the layer (exclusively at most one layer per node)
        /// </summary>
        public UFBXList<UFBXNode> nodes;

        /// <summary>
        /// Contained nodes are visible
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool visible;

        /// <summary>
        /// Contained nodes cannot be edited
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool frozen;

        /// <summary>
        /// Visual color for UI
        /// </summary>
        public Vector3 UIColor;
    }

    /// <summary>
    /// Named set of nodes/geometry features to select.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXSelectionSet
    {
        public UFBXElement element;

        /// <summary>
        /// Included nodes and geometry features
        /// </summary>
        public UFBXList<UFBXSelectionNode> nodes;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXSelectionNode
    {
        public UFBXElement element;

        /// <summary>
        /// Selection targets, possibly `NULL`
        /// </summary>
        public UFBXNode* targetNode;

        /// <summary>
        /// Selection targets, possibly `NULL`
        /// </summary>
        public UFBXMesh* targetMesh;

        /// <summary>
        /// Is `target_node` included in the selection
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool includeNode;

        /// <summary>
        /// Indices to selected components.
        /// Guaranteed to be valid as per `ufbx_load_opts.index_error_handling`
        /// if `target_mesh` is not `NULL`.
        /// Indices to `ufbx_mesh.vertices`
        /// </summary>
        public UFBXList<uint> vertices;
        /// <summary>
        /// Indices to selected components.
        /// Guaranteed to be valid as per `ufbx_load_opts.index_error_handling`
        /// if `target_mesh` is not `NULL`.
        /// Indices to `ufbx_mesh.edges`
        /// </summary>
        public UFBXList<uint> edges;
        /// <summary>
        /// Indices to selected components.
        /// Guaranteed to be valid as per `ufbx_load_opts.index_error_handling`
        /// if `target_mesh` is not `NULL`.
        /// Indices to `ufbx_mesh.faces`
        /// </summary>
        public UFBXList<uint> faces;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXCharacter
    {
        public UFBXelement element;
    }

    /// <summary>
    /// Target to follow with a constraint
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXConstraintTarget
    {
        /// <summary>
        /// Target node reference
        /// </summary>
        public UFBXNode* node;

        /// <summary>
        /// Relative weight to other targets (does not always sum to 1)
        /// </summary>
        public float weight;

        /// <summary>
        /// Offset from the actual target
        /// </summary>
        public UFBXTransform transform;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXConstraint
    {
        public UFBXElement element;

        /// <summary>
        /// Type of constraint to use
        /// </summary>
        public UFBXConstraintType type;

        /// <summary>
        /// Type of constraint to use
        /// </summary>
        public UFBXString typeName;

        /// <summary>
        /// Node to be constrained
        /// </summary>
        public UFBXNode* node;

        /// <summary>
        /// List of weighted targets for the constraint (pole vectors for IK)
        /// </summary>
        public UFBXList<UFBXConstraintTarget> targets;

        /// <summary>
        /// State of the constraint
        /// </summary>
        public float weight;

        /// <summary>
        /// State of the constraint
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool active;

        /// <summary>
        /// Translation/rotation/scale axes the constraint is applied to
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool constrainTranslationX;
        /// <summary>
        /// Translation/rotation/scale axes the constraint is applied to
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool constrainTranslationY;
        /// <summary>
        /// Translation/rotation/scale axes the constraint is applied to
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool constrainTranslationZ;

        /// <summary>
        /// Translation/rotation/scale axes the constraint is applied to
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool constrainRotationX;
        /// <summary>
        /// Translation/rotation/scale axes the constraint is applied to
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool constrainRotationY;
        /// <summary>
        /// Translation/rotation/scale axes the constraint is applied to
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool constrainRotationZ;

        /// <summary>
        /// Translation/rotation/scale axes the constraint is applied to
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool constrainScaleX;
        /// <summary>
        /// Translation/rotation/scale axes the constraint is applied to
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool constrainScaleY;
        /// <summary>
        /// Translation/rotation/scale axes the constraint is applied to
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool constrainScaleZ;

        /// <summary>
        /// Offset from the constrained position
        /// </summary>
        public UFBXTransform transformOffset;

        /// <summary>
        /// AIM: Target and up vectors
        /// </summary>
        public Vector3 aimVector;

        /// <summary>
        /// AIM: Target and up vectors
        /// </summary>
        public UFBXConstraintAimUpType aimUpType;

        /// <summary>
        /// AIM: Target and up vectors
        /// </summary>
        public UFBXNode* aimUpNode;

        /// <summary>
        /// AIM: Target and up vectors
        /// </summary>
        public Vector3 aimUpVector;

        /// <summary>
        /// SINGLE_CHAIN_IK: Target for the IK, `targets` contains pole vectors!
        /// </summary>
        public UFBXNode* IKEfector;
        /// <summary>
        /// SINGLE_CHAIN_IK: Target for the IK, `targets` contains pole vectors!
        /// </summary>
        public UFBXNode* IKEndNode;
        /// <summary>
        /// SINGLE_CHAIN_IK: Target for the IK, `targets` contains pole vectors!
        /// </summary>
        public Vector3 IKPoleVector;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXAudioLayer
    {
        public UFBXElement element;

        /// <summary>
        /// Clips contained in this layer.
        /// </summary>
        public UFBXList<UFBXAudioClip> clips;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXAudioClip
    {
        public UFBXElement element;

        /// <summary>
        /// Filename relative to the currently loaded file.
        /// HINT: If using functions other than `ufbx_load_file()`, you can provide
        /// `ufbx_load_opts.filename/raw_filename` to let ufbx resolve this.
        /// </summary>
        public UFBXString fileName;

        /// <summary>
        /// Absolute filename specified in the file.
        /// </summary>
        public UFBXString absoluteFileName;

        /// <summary>
        /// Relative filename specified in the file.
        /// NOTE: May be absolute if the file is saved in a different drive.
        /// </summary>
        public UFBXString relativeFileName;

        /// <summary>
        /// Filename relative to the loaded file, non-UTF-8 encoded.
        /// HINT: If using functions other than `ufbx_load_file()`, you can provide
        /// `ufbx_load_opts.filename/raw_filename` to let ufbx resolve this.
        /// </summary>
        public UFBXBlob rawFileName;

        /// <summary>
        /// Absolute filename specified in the file, non-UTF-8 encoded.
        /// </summary>
        public UFBXBlob rawAbsoluteFileName;

        /// <summary>
        /// Relative filename specified in the file, non-UTF-8 encoded.
        /// NOTE: May be absolute if the file is saved in a different drive.
        /// </summary>
        public UFBXBlob rawRelativeFileName;

        /// <summary>
        /// Optional embedded content blob
        /// </summary>
        public UFBXBlob content;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXBonePose
    {
        /// <summary>
        /// Node to apply the pose to.
        /// </summary>
        public UFBXNode* boneNode;

        /// <summary>
        /// Matrix from node local space to world space.
        /// </summary>
        public UFBXMatrix boneToWorld;

        /// <summary>
        /// Matrix from node local space to parent space.
        /// NOTE: FBX only stores world transformations so this is approximated from
        /// the parent world transform.
        /// </summary>
        public UFBXMatrix boneToParent;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXPose
    {
        public UFBXElement element;

        /// <summary>
        /// Set if this pose is marked as a bind pose.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool isBindPose;

        /// <summary>
        /// List of bone poses.
        /// Sorted by `ufbx_node.typed_id`.
        /// </summary>
        public UFBXList<UFBXBonePose> bonePoses;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXMetadataObject
    {
        public UFBXElement element;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXNameElement
    {
        public UFBXString name;
        public UFBXElementType type;

        public uint internalKey;

        public UFBXElement* element;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXApplication
    {
        public UFBXString vendor;
        public UFBXString name;
        public UFBXString version;
    }

    /// <summary>
    /// Warning about a non-fatal issue in the file.
    /// Often contains information about issues that ufbx has corrected about the
    /// file but it might indicate something is not working properly.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXWarning
    {
        /// <summary>
        /// Type of the warning.
        /// </summary>
        public UFBXWarningType type;
        /// <summary>
        /// Description of the warning.
        /// </summary>
        public UFBXString description;
        /// <summary>
        /// The element related to this warning or `UFBX_NO_INDEX` if not related to a specific element.
        /// </summary>
        public uint elementID;
        /// <summary>
        /// Number of times this warning was encountered.
        /// </summary>
        public ulong count;
    }

    /// <summary>
    /// Embedded thumbnail in the file, valid if the dimensions are non-zero.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXThumbnail
    {
        public UFBXProps props;

        /// <summary>
        /// Extents of the thumbnail
        /// </summary>
        public uint width;
        /// <summary>
        /// Extents of the thumbnail
        /// </summary>
        public uint height;

        /// <summary>
        /// Format of `ufbx_thumbnail.data`.
        /// </summary>
        public UFBXThumbnailFormat format;

        /// <summary>
        /// Thumbnail pixel data, layout as contiguous rows from bottom to top.
        /// See `ufbx_thumbnail.format` for the pixel format.
        /// </summary>
        public UFBXBlob data;
    }

    /// <summary>
    /// Miscellaneous data related to the loaded file
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXMetadata
    {
        /// <summary>
        /// List of non-fatal warnings about the file.
        /// If you need to only check whether a specific warning was triggered you
        /// can use `ufbx_metadata.has_warning[]`.
        /// </summary>
        public UFBXList<UFBXWarning> warnings;

        /// <summary>
        /// FBX ASCII file format.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool ascii;

        /// <summary>
        /// FBX version in integer format, eg. 7400 for 7.4.
        /// </summary>
        public uint version;

        /// <summary>
        /// File format of the source file.
        /// </summary>
        public UFBXFileFormat fileFormat;

        /// <summary>
        /// Index arrays may contain `UFBX_NO_INDEX` instead of a valid index
        /// to indicate gaps.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool mayContainNoIndex;

        /// <summary>
        /// May contain meshes with no defined vertex position.
        /// NOTE: `ufbx_mesh.vertex_position.exists` may be `false`!
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool mayContainMissingVertexPosition;

        /// <summary>
        /// Arrays may contain items with `NULL` element references.
        /// See `ufbx_load_opts.connect_broken_elements`.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool mayContainBrokenElements;

        /// <summary>
        /// Some API guarantees do not apply (depending on unsafe options used).
        /// Loaded with `ufbx_load_opts.allow_unsafe` enabled.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool isUnsafe;

        /// <summary>
        /// Flag for each possible warning type.
        /// See `ufbx_metadata.warnings[]` for detailed warning information.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool hasWarningMissingExternalFile;

        /// <summary>
        /// Flag for each possible warning type.
        /// See `ufbx_metadata.warnings[]` for detailed warning information.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool hasWarningImplicitMTL;

        /// <summary>
        /// Flag for each possible warning type.
        /// See `ufbx_metadata.warnings[]` for detailed warning information.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool hasWarningTruncatedArray;

        /// <summary>
        /// Flag for each possible warning type.
        /// See `ufbx_metadata.warnings[]` for detailed warning information.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool hasWarningMissingGeometryData;

        /// <summary>
        /// Flag for each possible warning type.
        /// See `ufbx_metadata.warnings[]` for detailed warning information.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool hasWarningDuplicateConnection;

        /// <summary>
        /// Flag for each possible warning type.
        /// See `ufbx_metadata.warnings[]` for detailed warning information.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool hasWarningBadVertexWAttribute;

        /// <summary>
        /// Flag for each possible warning type.
        /// See `ufbx_metadata.warnings[]` for detailed warning information.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool hasWarningMissingPolygonMapping;

        /// <summary>
        /// Flag for each possible warning type.
        /// See `ufbx_metadata.warnings[]` for detailed warning information.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool hasWarningUnsupportedVersion;

        /// <summary>
        /// Flag for each possible warning type.
        /// See `ufbx_metadata.warnings[]` for detailed warning information.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool hasWarningIndexClamped;

        /// <summary>
        /// Flag for each possible warning type.
        /// See `ufbx_metadata.warnings[]` for detailed warning information.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool hasWarningBadUnicode;

        /// <summary>
        /// Flag for each possible warning type.
        /// See `ufbx_metadata.warnings[]` for detailed warning information.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool hasWarningBadBase64Content;

        /// <summary>
        /// Flag for each possible warning type.
        /// See `ufbx_metadata.warnings[]` for detailed warning information.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool hasWarningBadElementConnectedToRoot;

        /// <summary>
        /// Flag for each possible warning type.
        /// See `ufbx_metadata.warnings[]` for detailed warning information.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool hasWarningDuplicateObjectID;

        /// <summary>
        /// Flag for each possible warning type.
        /// See `ufbx_metadata.warnings[]` for detailed warning information.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool hasWarningEmptyFaceRemoved;

        /// <summary>
        /// Flag for each possible warning type.
        /// See `ufbx_metadata.warnings[]` for detailed warning information.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool hasWarningUnknownObjDirective;

        public UFBXString creator;

        [MarshalAs(UnmanagedType.I1)]
        public bool bigEndian;

        public UFBXString fileName;

        public UFBXString relativeRoot;

        public UFBXBlob rawFileName;

        public UFBXBlob rawRelativeRoot;

        public UFBXExporter exporter;

        public uint exporterVersion;

        public UFBXProps sceneProps;

        public UFBXApplication originalApplication;
        public UFBXApplication latestApplication;

        public UFBXThumbnail thumbnail;

        [MarshalAs(UnmanagedType.I1)]
        public bool geometryIgnored;

        [MarshalAs(UnmanagedType.I1)]
        public bool animationIgnored;

        [MarshalAs(UnmanagedType.I1)]
        public bool embeddedIgnored;

        public ulong maxFaceTriangles;

        public ulong resultMemoryUsed;
        public ulong tempMemoryUsed;
        public ulong resultAllocs;
        public ulong tempAllocs;

        public ulong elementBufferSize;
        public ulong shaderTextureCount;

        public float bonePropSizeUnit;
        [MarshalAs(UnmanagedType.I1)]
        public bool bonePropLimbLengthRelative;

        public float orthoSizeUnit;

        /// <summary>
        /// One second in internal KTime units
        /// </summary>
        public long ktimeSecond;

        public UFBXString originalFilePath;

        public UFBXBlob rawOriginalFilePath;

        /// <summary>
        /// Space conversion method used on the scene.
        /// </summary>
        public UFBXSpaceConversion spaceConversion;

        /// <summary>
        /// Transform that has been applied to root for axis/unit conversion.
        /// </summary>
        public Quaternion rootRotation;
        /// <summary>
        /// Transform that has been applied to root for axis/unit conversion.
        /// </summary>
        public float rootScale;

        /// <summary>
        /// Axis that the scene has been mirrored by.
        /// All geometry has been mirrored in this axis.
        /// </summary>
        public UFBXMirrorAxis mirrorAxis;

        /// <summary>
        /// Amount geometry has been scaled.
        /// See `UFBX_SPACE_CONVERSION_MODIFY_GEOMETRY`.
        /// </summary>
        public float geometryScale;
    }

    /// <summary>
    /// Global settings: Axes and time/unit scales
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXSceneSettings
    {
        public UFBXProps props;

        /// <summary>
        /// Mapping of X/Y/Z axes to world-space directions.
        /// HINT: Use `ufbx_load_opts.target_axes` to normalize this.
        /// NOTE: This contains the _original_ axes even if you supply `ufbx_load_opts.target_axes`.
        /// </summary>
        public UFBXCoordinateAxes axes;

        /// <summary>
        /// How many meters does a single world-space unit represent.
        /// FBX files usually default to centimeters, reported as `0.01` here.
        /// HINT: Use `ufbx_load_opts.target_unit_meters` to normalize this.
        /// </summary>
        public float unitMeters;

        /// <summary>
        /// Frames per second the animation is defined at.
        /// </summary>
        public double framesPerSecond;
        
        public Vector3 ambientColor;
        public UFBXString defaultCamera;

        /// <summary>
        /// Animation user interface settings.
        /// HINT: Use `ufbx_scene_settings.frames_per_second` instead of interpreting these yourself.
        /// </summary>
        public UFBXTimeMode timeMode;
        /// <summary>
        /// Animation user interface settings.
        /// HINT: Use `ufbx_scene_settings.frames_per_second` instead of interpreting these yourself.
        /// </summary>
        public UFBXTimeProtocol timeProtocol;
        /// <summary>
        /// Animation user interface settings.
        /// HINT: Use `ufbx_scene_settings.frames_per_second` instead of interpreting these yourself.
        /// </summary>
        public UFBXSnapMode snapMode;

        /// <summary>
        /// Original settings (?)
        /// </summary>
        public UFBXCoordinateAxis originalAxisUp;
        /// <summary>
        /// Original settings (?)
        /// </summary>
        public float originalUnitMeters;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXScene
    {
        public UFBXMetadata metadata;

        /// <summary>
        /// Global settings
        /// </summary>
        public UFBXSceneSettings settings;

        /// <summary>
        /// Node instances in the scene
        /// </summary>
        public UFBXNode* rootNode;

        /// <summary>
        /// Default animation descriptor
        /// </summary>
        public UFBXAnim* anim;

        public UFBXList<UFBXUnknown> unknowns;

        /// <summary>
        /// Nodes
        /// </summary>
        public UFBXList<UFBXNode> nodes;

        /// <summary>
        /// Node attributes (common)
        /// </summary>
        public UFBXList<UFBXMesh> meshes;
        /// <summary>
        /// Node attributes (common)
        /// </summary>
        public UFBXList<UFBXLight> lights;
        /// <summary>
        /// Node attributes (common)
        /// </summary>
        public UFBXList<UFBXCamera> cameras;
        /// <summary>
        /// Node attributes (common)
        /// </summary>
        public UFBXList<UFBXBone> bones;
        /// <summary>
        /// Node attributes (common)
        /// </summary>
        public UFBXList<UFBXEmpty> empties;

        /// <summary>
        /// Node attributes (curves/surfaces)
        /// </summary>
        public UFBXList<UFBXLineCurve> lineCurves;
        /// <summary>
        /// Node attributes (curves/surfaces)
        /// </summary>
        public UFBXList<UFBXNURBSCurve> NURBSCurves;
        /// <summary>
        /// Node attributes (curves/surfaces)
        /// </summary>
        public UFBXList<UFBXNURBSSurface> NURBSSurfaces;
        /// <summary>
        /// Node attributes (curves/surfaces)
        /// </summary>
        public UFBXList<UFBXNURBSTrimSurface> NURBSTrimSurfaces;
        /// <summary>
        /// Node attributes (curves/surfaces)
        /// </summary>
        public UFBXList<UFBXNURBSTrimBoundary> NURBSTrimBoundaries;

        /// <summary>
        /// Node attributes (advanced)
        /// </summary>
        public UFBXList<UFBXProceduralGeometry> proceduralGeometries;
        /// <summary>
        /// Node attributes (advanced)
        /// </summary>
        public UFBXList<UFBXStereoCamera> stereoCameras;
        /// <summary>
        /// Node attributes (advanced)
        /// </summary>
        public UFBXList<UFBXCameraSwitcher> cameraSwitchers;
        /// <summary>
        /// Node attributes (advanced)
        /// </summary>
        public UFBXList<UFBXMarker> markers;
        /// <summary>
        /// Node attributes (advanced)
        /// </summary>
        public UFBXList<UFBXLODGroup> lodGroups;

        /// <summary>
        /// Deformers
        /// </summary>
        public UFBXList<UFBXSkinDeformer> skinDeformers;
        /// <summary>
        /// Deformers
        /// </summary>
        public UFBXList<UFBXSkinCluster> skinClusters;
        /// <summary>
        /// Deformers
        /// </summary>
        public UFBXList<UFBXBlendDeformer> blendDeformers;
        /// <summary>
        /// Deformers
        /// </summary>
        public UFBXList<UFBXBlendChannel> blendChannels;
        /// <summary>
        /// Deformers
        /// </summary>
        public UFBXList<UFBXBlendShape> blendShapes;
        /// <summary>
        /// Deformers
        /// </summary>
        public UFBXList<UFBXCacheDeformer> cacheDeformers;
        /// <summary>
        /// Deformers
        /// </summary>
        public UFBXList<UFBXCacheFile> cacheFiles;

        /// <summary>
        /// Materials
        /// </summary>
        public UFBXList<UFBXMaterial> materials;
        /// <summary>
        /// Materials
        /// </summary>
        public UFBXList<UFBXTexture> textures;
        /// <summary>
        /// Materials
        /// </summary>
        public UFBXList<UFBXVideo> videos;
        /// <summary>
        /// Materials
        /// </summary>
        public UFBXList<UFBXShader> shaders;
        /// <summary>
        /// Materials
        /// </summary>
        public UFBXList<UFBXShaderBinding> shaderBindings;

        /// <summary>
        /// Animation
        /// </summary>
        public UFBXList<UFBXAnimStack> animStacks;
        /// <summary>
        /// Animation
        /// </summary>
        public UFBXList<UFBXAnimLayer> animLayers;
        /// <summary>
        /// Animation
        /// </summary>
        public UFBXList<UFBXAnimValue> animValues;
        /// <summary>
        /// Animation
        /// </summary>
        public UFBXList<UFBXAnimCurve> animCurves;

        /// <summary>
        /// Collections
        /// </summary>
        public UFBXList<UFBXDisplayLayer> displayLayers;
        /// <summary>
        /// Collections
        /// </summary>
        public UFBXList<UFBXSelectionSet> selectionSets;
        /// <summary>
        /// Collections
        /// </summary>
        public UFBXList<UFBXSelectionNode> selectionNodes;

        /// <summary>
        /// Constraints
        /// </summary>
        public UFBXList<UFBXCharacter> characters;
        /// <summary>
        /// Constraints
        /// </summary>
        public UFBXList<UFBXConstraint> constraints;

        /// <summary>
        /// Audio
        /// </summary>
        public UFBXList<UFBXAudioLayer> audioLayers;
        /// <summary>
        /// Audio
        /// </summary>
        public UFBXList<UFBXAudioClip> audioClips;

        /// <summary>
        /// Miscellaneous
        /// </summary>
        public UFBXList<UFBXPose> poses;
        /// <summary>
        /// Miscellaneous
        /// </summary>
        public UFBXList<UFBXMetadataObject> metadataObjects;

        /// <summary>
        /// Unique texture files referenced by the scene.
        /// </summary>
        public UFBXList<UFBXTextureFile> textureFiles;

        /// <summary>
        /// All elements and connections in the whole file
        /// Sorted by `id`
        /// </summary>
        public UFBXList<UFBXElement> elements;

        /// <summary>
        /// All elements and connections in the whole file
        /// Sorted by `src,src_prop`
        /// </summary>
        public UFBXList<UFBXConnection> connectionsSource;
        /// <summary>
        /// All elements and connections in the whole file
        /// Sorted by `dst,dst_prop`
        /// </summary>
        public UFBXList<UFBXConnection> connectionsDestination;

        /// <summary>
        /// Elements sorted by name, type
        /// </summary>
        public UFBXList<UFBXNameElement> elementsByName;

        /// <summary>
        /// Enabled if `ufbx_load_opts.retain_dom == true`.
        /// </summary>
        public UFBXDomNode* domRoot;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXCurvePoint
    {
        [MarshalAs(UnmanagedType.I1)]
        public bool valid;
        public Vector3 position;
        public Vector3 derivative;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXSurfacePoint
    {
        [MarshalAs(UnmanagedType.I1)]
        public bool valid;
        public Vector3 position;
        public Vector3 derivativeU;
        public Vector3 derivativeV;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXTopoEdge
    {
        /// <summary>
        /// Starting index of the edge, always defined
        /// </summary>
        public uint index;
        /// <summary>
        /// Ending index of the edge / next per-face `ufbx_topo_edge`, always defined
        /// </summary>
        public uint next;
        /// <summary>
        /// Previous per-face `ufbx_topo_edge`, always defined
        /// </summary>
        public uint prev;
        /// <summary>
        /// `ufbx_topo_edge` on the opposite side, `UFBX_NO_INDEX` if not found
        /// </summary>
        public uint twin;
        /// <summary>
        /// Index into `mesh->faces[]`, always defined
        /// </summary>
        public uint face;
        /// <summary>
        /// Index into `mesh->edges[]`, `UFBX_NO_INDEX` if not found
        /// </summary>
        public uint edge;

        public UFBXTopoFlags flags;
    }

    /// <summary>
    /// Vertex data array for `ufbx_generate_indices()`.
    /// NOTE: `ufbx_generate_indices()` compares the vertices using `memcmp()`, so
    /// any padding should be cleared to zero.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXVertexStream
    {
        /// <summary>
        /// Data pointer of shape `char[vertex_count][vertex_size]`.
        /// </summary>
        public nint data;

        /// <summary>
        /// Number of vertices in this stream, for sanity checking.
        /// </summary>
        public ulong vertexCount;

        /// <summary>
        /// Size of a vertex in bytes.
        /// </summary>
        public ulong vertexSize;
    }

    /// <summary>
    /// Allocator callbacks and user context
    /// NOTE: The allocator will be stored to the loaded scene and will be called
    /// again from `ufbx_free_scene()` so make sure `user` outlives that!
    /// You can use `free_allocator_fn()` to free the allocator yourself.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXAllocator
    {
        public delegate* unmanaged[Cdecl]<void*, ulong, void*> allocFunction;
        public delegate* unmanaged[Cdecl]<void *, void *, ulong, ulong, void *> reallocFunction;
        public delegate* unmanaged[Cdecl]<void *, void *, ulong, void> freeFunction;
        public delegate* unmanaged[Cdecl]<void *, void> freeAllocatorFunction;

        public nint user;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXAllocatorOpts
    {
        /// <summary>
        /// Allocator callbacks
        /// </summary>
        public UFBXAllocator allocator;

        /// <summary>
        /// Maximum number of bytes to allocate before failing
        /// </summary>
        public ulong memoryLimit;

        /// <summary>
        /// Maximum number of allocations to attempt before failing
        /// </summary>
        public ulong allocationLimit;

        /// <summary>
        /// Threshold to swap from batched allocations to individual ones
        /// Defaults to 1MB if set to zero
        /// NOTE: If set to `1` ufbx will allocate everything in the smallest
        /// possible chunks which may be useful for debugging (eg. ASAN)
        /// </summary>
        public ulong hugeThreshold;

        /// <summary>
        /// Maximum size of a single allocation containing sub-allocations.
        /// Defaults to 16MB if set to zero
        /// The maximum amount of wasted memory depends on `max_chunk_size` and
        /// `huge_threshold`: each chunk can waste up to `huge_threshold` bytes
        /// internally and the last chunk might be incomplete. So for example
        /// with the defaults we can waste around 1MB/16MB = 6.25% overall plus
        /// up to 32MB due to the two incomplete blocks. The actual amounts differ
        /// slightly as the chunks start out at 4kB and double in size each time,
        /// meaning that the maximum fixed overhead (up to 32MB with defaults) is
        /// at most ~30% of the total allocation size.
        /// </summary>
        public ulong maxChunkSize;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXStream
    {
        /// <summary>
        /// Required
        /// </summary>
        public delegate* unmanaged[Cdecl]<void *, void *, ulong, ulong> readFunction;

        /// <summary>
        /// Optional: Will use `read_fn()` if missing
        /// </summary>
        public delegate* unmanaged[Cdecl]<void *, ulong, byte> skipFunction;

        /// <summary>
        /// Optional
        /// </summary>
        public delegate* unmanaged[Cdecl]<void *, ulong> sizeFunction;

        /// <summary>
        /// Optional
        /// </summary>
        public delegate* unmanaged[Cdecl]<void *, void> closeFunction;

        /// <summary>
        /// Context passed to other functions
        /// </summary>
        public nint user;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXOpenFileInfo
    {
        /// <summary>
        /// Context that can be passed to the following functions to use a shared allocator:
        ///   ufbx_open_file_ctx()
        ///   ufbx_open_memory_ctx()
        /// </summary>
        public ulong context;

        /// <summary>
        /// Kind of file to load.
        /// </summary>
        public UFBXOpenFileType type;

        /// <summary>
        /// Original filename in the file, not resolved or UTF-8 encoded.
        /// NOTE: Not necessarily NULL-terminated!
        /// </summary>
        public UFBXBlob originalFileName;
    }

    /// <summary>
    /// Options for `ufbx_open_file()`.
    /// </summary>
    public struct UFBXOpenFileOpts
    {
        public uint beginZero;

        /// <summary>
        /// Allocator to allocate the memory with.
        /// </summary>
        public UFBXAllocatorOpts allocator;

        /// <summary>
        /// The filename is guaranteed to be NULL-terminated.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool filenameNullTerminated;

        public uint endZero;
    }

    /// <summary>
    /// Detailed error stack frame.
    /// NOTE: You must compile `ufbx.c` with `UFBX_ENABLE_ERROR_STACK` to enable the error stack.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXErrorFrame
    {
        public uint sourceLine;
        public UFBXString function;
        public UFBXString description;
    }

    /// <summary>
    /// Error description with detailed stack trace
    /// HINT: You can use `ufbx_format_error()` for formatting the error
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXError
    {
        /// <summary>
        /// Type of the error, or `UFBX_ERROR_NONE` if successful.
        /// </summary>
        public UFBXErrorType type;

        /// <summary>
        /// Description of the error type.
        /// </summary>
        public UFBXString description;

        /// <summary>
        /// Internal error stack.
        /// NOTE: You must compile `ufbx.c` with `UFBX_ENABLE_ERROR_STACK` to enable the error stack.
        /// </summary>
        public uint stackSize;

        /// <summary>
        /// Internal error stack.
        /// NOTE: You must compile `ufbx.c` with `UFBX_ENABLE_ERROR_STACK` to enable the error stack.
        /// </summary>
        public UFBXErrorFrame stack0;
        /// <summary>
        /// Internal error stack.
        /// NOTE: You must compile `ufbx.c` with `UFBX_ENABLE_ERROR_STACK` to enable the error stack.
        /// </summary>
        public UFBXErrorFrame stack1;
        /// <summary>
        /// Internal error stack.
        /// NOTE: You must compile `ufbx.c` with `UFBX_ENABLE_ERROR_STACK` to enable the error stack.
        /// </summary>
        public UFBXErrorFrame stack2;
        /// <summary>
        /// Internal error stack.
        /// NOTE: You must compile `ufbx.c` with `UFBX_ENABLE_ERROR_STACK` to enable the error stack.
        /// </summary>
        public UFBXErrorFrame stack3;
        /// <summary>
        /// Internal error stack.
        /// NOTE: You must compile `ufbx.c` with `UFBX_ENABLE_ERROR_STACK` to enable the error stack.
        /// </summary>
        public UFBXErrorFrame stack4;
        /// <summary>
        /// Internal error stack.
        /// NOTE: You must compile `ufbx.c` with `UFBX_ENABLE_ERROR_STACK` to enable the error stack.
        /// </summary>
        /// <summary>
        /// Internal error stack.
        /// NOTE: You must compile `ufbx.c` with `UFBX_ENABLE_ERROR_STACK` to enable the error stack.
        /// </summary>
        public UFBXErrorFrame stack5;
        /// <summary>
        /// Internal error stack.
        /// NOTE: You must compile `ufbx.c` with `UFBX_ENABLE_ERROR_STACK` to enable the error stack.
        /// </summary>
        public UFBXErrorFrame stack6;
        /// <summary>
        /// Internal error stack.
        /// NOTE: You must compile `ufbx.c` with `UFBX_ENABLE_ERROR_STACK` to enable the error stack.
        /// </summary>
        public UFBXErrorFrame stack7;

        /// <summary>
        /// Additional error information, such as missing file filename.
        /// `info` is a NULL-terminated UTF-8 string containing `info_length` bytes, excluding the trailing `'\0'`.
        /// </summary>
        public ulong infoLength;
        /// <summary>
        /// Additional error information, such as missing file filename.
        /// `info` is a NULL-terminated UTF-8 string containing `info_length` bytes, excluding the trailing `'\0'`.
        /// </summary>
        public char infoStart;

        public string Info
        {
            get
            {
                if (infoLength == 0)
                {
                    return "";
                }

                unsafe
                {
                    fixed(char *p = &infoStart)
                    {
                        var infoSpan = new Span<byte>((byte*)p, (int)infoLength);

                        return Encoding.UTF8.GetString(infoSpan);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Loading progress information.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXProgress
    {
        public ulong bytesRead;
        public ulong bytesTotal;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXInflateInput
    {
    }

}
