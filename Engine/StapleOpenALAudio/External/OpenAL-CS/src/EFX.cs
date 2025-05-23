#region License
/* OpenAL# - C# Wrapper for OpenAL Soft
 *
 * Copyright (c) 2014-2015 Ethan Lee.
 *
 * This software is provided 'as-is', without any express or implied warranty.
 * In no event will the authors be held liable for any damages arising from
 * the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 * claim that you wrote the original software. If you use this software in a
 * product, an acknowledgment in the product documentation would be
 * appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not be
 * misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source distribution.
 *
 * Ethan "flibitijibibo" Lee <flibitijibibo@flibitijibibo.com>
 *
 */
#endregion

#region Using Statements
using System;
using System.Runtime.InteropServices;
#endregion

namespace OpenAL
{
	public static partial class EFX
	{
		/* typedef int ALenum; */
		public const int AL_METERS_PER_UNIT =				0x20004;

		public const int AL_DIRECT_FILTER =				0x20005;
		public const int AL_AUXILIARY_SEND_FILTER =			0x20006;
		public const int AL_AIR_ABSORPTION_FACTOR =			0x20007;
		public const int AL_ROOM_ROLLOFF_FACTOR =			0x20008;
		public const int AL_CONE_OUTER_GAINHF =				0x20009;
		public const int AL_DIRECT_FILTER_GAINHF_AUTO =			0x2000A;
		public const int AL_AUXILIARY_SEND_FILTER_GAIN_AUTO =		0x2000B;
		public const int AL_AUXILIARY_SEND_FILTER_GAINHF_AUTO =		0x2000C;

		public const int AL_REVERB_DENSITY =				0x0001;
		public const int AL_REVERB_DIFFUSION =				0x0002;
		public const int AL_REVERB_GAIN =				0x0003;
		public const int AL_REVERB_GAINHF =				0x0004;
		public const int AL_REVERB_DECAY_TIME =				0x0005;
		public const int AL_REVERB_DECAY_HFRATIO =			0x0006;
		public const int AL_REVERB_REFLECTIONS_GAIN =			0x0007;
		public const int AL_REVERB_REFLECTIONS_DELAY =			0x0008;
		public const int AL_REVERB_LATE_REVERB_GAIN =			0x0009;
		public const int AL_REVERB_LATE_REVERB_DELAY =			0x000A;
		public const int AL_REVERB_AIR_ABSORPTION_FACTOR =		0x000B;
		public const int AL_REVERB_ROOM_ROLLOFF_FACTOR =		0x000C;
		public const int AL_REVERB_DECAY_HFLIMIT =			0x000D;

		public const int AL_EAXREVERB_DENSITY =				0x0001;
		public const int AL_EAXREVERB_DIFFUSION =			0x0002;
		public const int AL_EAXREVERB_GAIN =				0x0003;
		public const int AL_EAXREVERB_GAINHF =				0x0004;
		public const int AL_EAXREVERB_GAINLF =				0x0005;
		public const int AL_EAXREVERB_DECAY_TIME =			0x0006;
		public const int AL_EAXREVERB_DECAY_HFRATIO =			0x0007;
		public const int AL_EAXREVERB_DECAY_LFRATIO =			0x0008;
		public const int AL_EAXREVERB_REFLECTIONS_GAIN =		0x0009;
		public const int AL_EAXREVERB_REFLECTIONS_DELAY =		0x000A;
		public const int AL_EAXREVERB_REFLECTIONS_PAN =			0x000B;
		public const int AL_EAXREVERB_LATE_REVERB_GAIN =		0x000C;
		public const int AL_EAXREVERB_LATE_REVERB_DELAY =		0x000D;
		public const int AL_EAXREVERB_LATE_REVERT_PAN =			0x000E;
		public const int AL_EAXREVERB_ECHO_TIME =			0x000F;
		public const int AL_EAXREVERB_ECHO_DEPTH =			0x0010;
		public const int AL_EAXREVERB_MODULATION_TIME =			0x0011;
		public const int AL_EAXREVERB_MODULATION_DEPTH =		0x0012;
		public const int AL_EAXREVERB_AIR_ABSORPTION_GAINHF =		0x0013;
		public const int AL_EAXREVERB_HFREFERENCE =			0x0014;
		public const int AL_EAXREVERB_LFREFERENCE =			0x0015;
		public const int AL_EAXREVERB_ROOM_ROLLOFF_FACTOR =		0x0016;
		public const int AL_EAXREVERB_DECAY_HFLIMIT =			0x0017;

		public const int AL_CHORUS_WAVEFORM =				0x0001;
		public const int AL_CHORUS_PHASE =				0x0002;
		public const int AL_CHORUS_RATE =				0x0003;
		public const int AL_CHORUS_DEPTH =				0x0004;
		public const int AL_CHORUS_FEEDBACK =				0x0005;
		public const int AL_CHORUS_DELAY =				0x0006;

		public const int AL_DISTORTION_EDGE =				0x0001;
		public const int AL_DISTORTION_GAIN =				0x0002;
		public const int AL_DISTORTION_LOWPASS_CUTOFF =			0x0003;
		public const int AL_DISTORTION_EQCENTER =			0x0004;
		public const int AL_DISTORTION_EQBANDWIDTH =			0x0005;

		public const int AL_ECHO_DELAY =				0x0001;
		public const int AL_ECHO_LRDELAY =				0x0002;
		public const int AL_ECHO_DAMPING =				0x0003;
		public const int AL_ECHO_FEEDBACK =				0x0004;
		public const int AL_ECHO_SPREAD =				0x0005;

		public const int AL_FLANGER_WAVEFORM =				0x0001;
		public const int AL_FLANGER_PHASE =				0x0002;
		public const int AL_FLANGER_RATE =				0x0003;
		public const int AL_FLANGER_DEPTH =				0x0004;
		public const int AL_FLANGER_FEEDBACK =				0x0005;
		public const int AL_FLANGER_DELAY =				0x0006;

		public const int AL_FREQUENCY_SHIFTER_FREQUENCY =		0x0001;
		public const int AL_FREQUENCY_SHIFTER_LEFT_DIRECTION =		0x0002;
		public const int AL_FREQUENCY_SHIFTER_RIGHT_DIRECTION =		0x0003;

		public const int AL_VOCAL_MORPHER_PHONEMEA =			0x0001;
		public const int AL_VOCAL_MORPHER_PHONEMEA_COARSE_TUNING =	0x0002;
		public const int AL_VOCAL_MORPHER_PHONEMEB =			0x0003;
		public const int AL_VOCAL_MORPHER_PHONEMEB_COARSE_TUNING =	0x0004;
		public const int AL_VOCAL_MORPHER_WAVEFORM =			0x0005;
		public const int AL_VOCAL_MORPHER_RATE =			0x0006;

		public const int AL_PITCH_SHIFTER_COARSE_TUNE =			0x0001;
		public const int AL_PITCH_SHIFTER_FINE_TUNE =			0x0002;

		public const int AL_RING_MODULATOR_FREQUENCY =			0x0001;
		public const int AL_RING_MODULATOR_HIGHPASS_CUTOFF =		0x0002;
		public const int AL_RING_MODULATOR_WAVEFORM =			0x0003;

		public const int AL_AUTOWAH_ATTACK_TIME =			0x0001;
		public const int AL_AUTOWAH_RELEASE_TIME =			0x0002;
		public const int AL_AUTOWAH_RESONANCE =				0x0003;
		public const int AL_AUTOWAH_PEAK_GAIN =				0x0004;

		public const int AL_COMPRESSOR_ONOFF =				0x0001;

		public const int AL_EQUALIZER_LOW_GAIN =			0x0001;
		public const int AL_EQUALIZER_LOW_CUTOFF =			0x0002;
		public const int AL_EQUALIZER_MID1_GAIN =			0x0003;
		public const int AL_EQUALIZER_MID1_CENTER =			0x0004;
		public const int AL_EQUALIZER_MID1_WIDTH =			0x0005;
		public const int AL_EQUALIZER_MID2_GAIN =			0x0006;
		public const int AL_EQUALIZER_MID2_CENTER =			0x0007;
		public const int AL_EQUALIZER_MID2_WIDTH =			0x0008;
		public const int AL_EQUALIZER_HIGH_GAIN =			0x0009;
		public const int AL_EQUALIZER_HIGH_CUTOFF =			0x000A;

		public const int AL_EFFECT_FIRST_PARAMETER =			0x0000;
		public const int AL_EFFECT_LAST_PARAMETER =			0x8000;
		public const int AL_EFFECT_TYPE =				0x8001;

		public const int AL_EFFECT_NULL =				0x0000;
		public const int AL_EFFECT_REVERB =				0x0001;
		public const int AL_EFFECT_CHORUS =				0x0002;
		public const int AL_EFFECT_DISTORTION =				0x0003;
		public const int AL_EFFECT_ECHO =				0x0004;
		public const int AL_EFFECT_FLANGER =				0x0005;
		public const int AL_EFFECT_FREQUENCY_SHIFTER =			0x0006;
		public const int AL_EFFECT_VOCAL_MORPHER =			0x0007;
		public const int AL_EFFECT_PITCH_SHIFTER =			0x0008;
		public const int AL_EFFECT_RING_MODULATOR =			0x0009;
		public const int AL_EFFECT_AUTOWAH =				0x000A;
		public const int AL_EFFECT_COMPRESSOR =				0x000B;
		public const int AL_EFFECT_EQUALIZER =				0x000C;
		public const int AL_EFFECT_EAXREVERB =				0x8000;

		public const int AL_EFFECTSLOT_EFFECT =				0x0001;
		public const int AL_EFFECTSLOT_GAIN =				0x0002;
		public const int AL_EFFECTSLOT_AUXILIARY_SEND_AUTO =		0x0003;

		public const int AL_EFFECTSLOT_NULL =				0x0000;

		public const int AL_LOWPASS_GAIN =				0x0001;
		public const int AL_LOWPASS_GAINHF =				0x0002;

		public const int AL_HIGHPASS_GAIN =				0x0001;
		public const int AL_HIGHPASS_GAINLF =				0x0002;

		public const int AL_BANDPASS_GAIN =				0x0001;
		public const int AL_BANDPASS_GAINLF =				0x0002;
		public const int AL_BANDPASS_GAINHF =				0x0003;

		public const int AL_FILTER_FIRST_PARAMETER =			0x0000;
		public const int AL_FILTER_LAST_PARAMETER =			0x8000;
		public const int AL_FILTER_TYPE =				0x8001;

		public const int AL_FILTER_NULL =				0x0000;
		public const int AL_FILTER_LOWPASS =				0x0001;
		public const int AL_FILTER_HIGHPASS =				0x0002;
		public const int AL_FILTER_BANDPASS =				0x0003;

		/* TODO: EFX Default Value Constants! */

		/* n refers to an ALsizei */
		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alGenEffects(int n, uint[] effects);

		/* n refers to an ALsizei. Overload provided to avoid uint[] alloc. */
		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alGenEffects(int n, out uint effects);

		/* n refers to an ALsizei */
		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alDeleteEffects(int n, uint[] effects);

		/* n refers to an ALsizei. Overload provided to avoid uint[] alloc. */
		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alDeleteEffects(int n, ref uint effects);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool alIsEffect(uint effect);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alEffecti(
			uint effect,
			int param,
			int iValue
		);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alEffectiv(
			uint effect,
			int param,
			int[] iValues
		);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alEffectf(
			uint effect,
			int param,
			float flValue
		);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alEffectfv(
			uint effect,
			int param,
			float[] flValues
		);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alGetEffecti(
			uint effect,
			int param,
			out int piValue
		);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alGetEffectiv(
			uint effect,
			int param,
			int[] piValues
		);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alGetEffectf(
			uint effect,
			int param,
			out float pflValue
		);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alGetEffectfv(
			uint effect,
			int param,
			float[] pflValues
		);

		/* n refers to an ALsizei */
		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alGenFilters(int n, uint[] filters);

		/* n refers to an ALsizei. Overload provided to avoid uint[] alloc. */
		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alGenFilters(int n, out uint filters);

		/* n refers to an ALsizei */
		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alDeleteFilters(int n, uint[] filters);

		/* n refers to an ALsizei. Overload provided to avoid uint[] alloc. */
		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alDeleteFilters(int n, ref uint filters);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool alIsFilter(uint filter);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alFilteri(
			uint filter,
			int param,
			int iValue
		);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alFilteriv(
			uint filter,
			int param,
			int[] iValues
		);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alFilterf(
			uint filter,
			int param,
			float flValue
		);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alFilterfv(
			uint filter,
			int param,
			float[] flValues
		);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alGetFilteri(
			uint filter,
			int param,
			out int piValue
		);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alGetFilteriv(
			uint filter,
			int param,
			int[] piValues
		);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alGetFilterf(
			uint filter,
			int param,
			out float pflValue
		);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alGetFilterfv(
			uint filter,
			int param,
			float[] pflValues
		);

		/* n refers to an ALsizei */
		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alGenAuxiliaryEffectSlots(
			int n,
			uint[] effectslots
		);

		/* n refers to an ALsizei. Overload provided to avoid uint[] alloc. */
		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alGenAuxiliaryEffectSlots(
			int n,
			out uint effectslots
		);

		/* n refers to an ALsizei */
		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alDeleteAuxiliaryEffectSlots(
			int n,
			uint[] effectslots
		);

		/* n refers to an ALsizei. Overload provided to avoid uint[] alloc. */
		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alDeleteAuxiliaryEffectSlots(
			int n,
			ref uint effectslots
		);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool alIsAuxiliaryEffectSlot(uint effectslot);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alAuxiliaryEffectSloti(
			uint effectslot,
			int param,
			int iValue
		);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alAuxiliaryEffectSlotiv(
			uint effectslot,
			int param,
			int[] iValues
		);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alAuxiliaryEffectSlotf(
			uint effectslot,
			int param,
			float flValue
		);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alAuxiliaryEffectSlotfv(
			uint effectslot,
			int param,
			float[] flValues
		);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alGetAuxiliaryEffectSloti(
			uint effectslot,
			int param,
			out int piValue
		);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alGetAuxiliaryEffectSlotiv(
			uint effectslot,
			int param,
			int[] piValues
		);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alGetAuxiliaryEffectSlotf(
			uint effectslot,
			int param,
			out float pflValue
		);

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void alGetAuxiliaryEffectSlotfv(
			uint effectslot,
			int param,
			float[] pflValues
		);
	}
}
