using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniInject;
using CSharpSynth.Effects;
using CSharpSynth.Sequencer;
using CSharpSynth.Synthesis;
using CSharpSynth.Midi;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

// An AudioSource is needed although it is not directly referenced.
// Furthermore, the AudioSource must have "Play on Awake" set to true.
[RequireComponent(typeof(AudioSource))]
public class MidiManager : MonoBehaviour, INeedInjection
{
    public static MidiManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<MidiManager>("MidiManager");
        }
    }

    public int MidiNoteVolume { get; set; } = 250;
    public int MidiInstrument { get; set; }

    // The txt file describing the instruments of the sound bank. Must be in a Resources folder.
    private readonly string bankFilePath = "GM Bank - Piano/GM Bank - Piano";
    private readonly int bufferSize = 1024;
    private readonly float gain = 1f;

    private float[] sampleBuffer;
    private MidiSequencer midiSequencer;
    private StreamSynthesizer midiStreamSynthesizer;

    private bool isInitialized;

    private void InitIfNotDoneYet()
    {
        if (isInitialized)
        {
            return;
        }

        midiStreamSynthesizer = new StreamSynthesizer(44100, 2, bufferSize, 16);
        sampleBuffer = new float[midiStreamSynthesizer.BufferSize];

        midiStreamSynthesizer.LoadBank(bankFilePath);
        midiSequencer = new MidiSequencer(midiStreamSynthesizer);

        isInitialized = true;
    }

    public void PlayMidiNote(int midiNote)
    {
        InitIfNotDoneYet();
        midiStreamSynthesizer.NoteOn(0, midiNote, MidiNoteVolume, MidiInstrument);
    }

    public void StopMidiNote(int midiNote)
    {
        InitIfNotDoneYet();
        midiStreamSynthesizer.NoteOff(0, midiNote);
    }

    // See http://unity3d.com/support/documentation/ScriptReference/MonoBehaviour.OnAudioFilterRead.html for reference code
    //	If OnAudioFilterRead is implemented, Unity will insert a custom filter into the audio DSP chain.
    //
    //	The filter is inserted in the same order as the MonoBehaviour script is shown in the inspector. 	
    //	OnAudioFilterRead is called everytime a chunk of audio is routed thru the filter (this happens frequently, every ~20ms depending on the samplerate and platform). 
    //	The audio data is an array of floats ranging from [-1.0f;1.0f] and contains audio from the previous filter in the chain or the AudioClip on the AudioSource. 
    //	If this is the first filter in the chain and a clip isn't attached to the audio source this filter will be 'played'. 
    //	That way you can use the filter as the audio clip, procedurally generating audio.
    //
    //	If OnAudioFilterRead is implemented a VU meter will show up in the inspector showing the outgoing samples level. 
    //	The process time of the filter is also measured and the spent milliseconds will show up next to the VU Meter 
    //	(it turns red if the filter is taking up too much time, so the mixer will starv audio data). 
    //	Also note, that OnAudioFilterRead is called on a different thread from the main thread (namely the audio thread) 
    //	so calling into many Unity functions from this function is not allowed ( a warning will show up ). 	
    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!isInitialized)
        {
            return;
        }

        // This uses the Unity specific float method we added to get the buffer
        midiStreamSynthesizer.GetNext(sampleBuffer);

        for (int i = 0; i < data.Length; i++)
        {
            data[i] = sampleBuffer[i] * gain;
        }
    }
}