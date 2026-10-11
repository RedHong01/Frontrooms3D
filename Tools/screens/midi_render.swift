// Offline MIDI renderer for the promo music (graphic-visual chat, 2026-10-08).
// Plays a JSON event list through Apple's multi-timbral GS synth (DLSMusicDevice, the system's
// gs_instruments.dls; nothing downloaded) in AVAudioEngine's OFFLINE manual rendering mode, so nothing is
// played on the speakers. Events are sent at sample-accurate chunk boundaries (64 frames, 1.3 ms).
// Usage: swiftc -O midi_render.swift -o <bin> && <bin> events.json out.wav seconds
// events.json: [[time_s, status, data1, data2], ...] sorted by time.
import AVFoundation
import AudioToolbox
import Foundation

let args = CommandLine.arguments
guard args.count >= 4, let seconds = Double(args[3]) else { print("usage: midi_render events.json out.wav seconds"); exit(2) }
let events = try JSONSerialization.jsonObject(with: Data(contentsOf: URL(fileURLWithPath: args[1]))) as! [[Double]]

let engine = AVAudioEngine()
let desc = AudioComponentDescription(componentType: kAudioUnitType_MusicDevice, componentSubType: kAudioUnitSubType_DLSSynth,
                                     componentManufacturer: kAudioUnitManufacturer_Apple, componentFlags: 0, componentFlagsMask: 0)
let synth = AVAudioUnitMIDIInstrument(audioComponentDescription: desc)
engine.attach(synth)
let fmt = AVAudioFormat(standardFormatWithSampleRate: 48000, channels: 2)!
engine.connect(synth, to: engine.mainMixerNode, format: fmt)
try engine.enableManualRenderingMode(.offline, format: fmt, maximumFrameCount: 4096)
try engine.start()

let total = AVAudioFramePosition(seconds * 48000.0)
let chunk: AVAudioFrameCount = 64
let buffer = AVAudioPCMBuffer(pcmFormat: engine.manualRenderingFormat, frameCapacity: chunk)!
let out = try AVAudioFile(forWriting: URL(fileURLWithPath: args[2]), settings: fmt.settings, commonFormat: .pcmFormatFloat32, interleaved: false)
var next = 0
while engine.manualRenderingSampleTime < total {
    let now = Double(engine.manualRenderingSampleTime) / 48000.0
    while next < events.count && events[next][0] <= now {
        let e = events[next]
        let status = UInt8(e[1]), d1 = UInt8(e[2]), d2 = UInt8(e[3])
        if status & 0xF0 == 0xC0 { synth.sendProgramChange(d1, onChannel: status & 0x0F) }
        else { synth.sendMIDIEvent(status, data1: d1, data2: d2) }
        next += 1
    }
    let n = min(chunk, AVAudioFrameCount(total - engine.manualRenderingSampleTime))
    let status = try engine.renderOffline(n, to: buffer)
    if status != .success { print("render status \(status)"); break }
    try out.write(from: buffer)
}
engine.stop()
print("rendered \(seconds) s, \(events.count) events -> \(args[2])")
