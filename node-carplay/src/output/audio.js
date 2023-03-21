const stream = require('stream')
const childProcess = require('child_process')
const chunker = require('stream-chunker');

const recordingStream = new stream.PassThrough()
const chunkedRecordingStream = chunker(320 * 8)
chunkedRecordingStream.pipe(recordingStream)

const startAplay = params => childProcess.spawn("/usr/bin/aplay", params)
const startArecord = params => childProcess.spawn("/usr/bin/arecord", params)

const sp1 = startAplay([
	'--device=plug:dsp0s',
	'--interactive',
	'--format=S16_LE',
	'--channels=2',
	'--rate=44100',
])
sp1.stderr.pipe(process.stdout)
sp1.stdout.pipe(process.stdout)

const sp2 = startAplay([
	'--device=plug:dsp1s',
	'--interactive',
	'--format=S16_LE',
	'--channels=2',
	'--rate=44100',
])
sp2.stderr.pipe(process.stdout)
sp2.stdout.pipe(process.stdout)

const microphoneParams = [
	// '--device=plughw:2,0',
	// '--period-time=16000',
	'--file-type=raw',
	'--format=S16_LE',
	'--channels=1',
	'--rate=16000',
];

process.on('exit', function() {
	sp1.kill();
	sp2.kill();
});


let microphone

module.exports = {
	sp1: sp1.stdin,
	sp2: sp2.stdin,
	microphone: recordingStream,
	startRecording: () => {
		microphone = startArecord(microphoneParams)
		// microphone.stdout.pipe(recordingStream)
		microphone.stdout.pipe(chunkedRecordingStream)
		microphone.stderr.pipe(process.stdout)
	},
	stopRecording: () => {
		microphone && microphone.stdout.unpipe(chunkedRecordingStream)
		microphone && microphone.kill()
		microphone = undefined
	},
}
