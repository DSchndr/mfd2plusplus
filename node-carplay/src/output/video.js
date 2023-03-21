const childProcess = require('child_process');
const fs = require('fs');

const fffmpeg = childProcess.spawn("/usr/bin/ffmpeg", [
	'-hide_banner',
	'-i', '-',
	'-threads', '8',
	'-framerate', '10',
	//'-bufsize', '16',
	//'-c:v', 'h264_mmal',
	'-vf', 'scale=400:230',
	'-pix_fmt', 'bgra',
	'-f', 'fbdev', '/dev/fb0',
]);

const ffmpeg = childProcess.spawn("/usr/bin/mpv", [
	'--profile=low-latency',
	'--video-latency-hacks=yes',
	//'--hwdec=mmal',
	'--no-cache',
	'--untimed',
	'--fps=30',
	'--no-terminal',
	'--no-correct-pts',
	'--no-input-default-bindings',
	'-',
]);

//const wstream = fs.createWriteStream('/tmp/video')
/* //Do not use 'have a shitty day' player cuz it crap.
const ffmpeg = childProcess.spawn("/usr/bin/omxplayer", [
	'-o', 'hdmi', '--live', '--no-keys', '-r', 'pipe:0'
]);*/

ffmpeg.stderr.pipe(process.stdout)
ffmpeg.stdout.pipe(process.stdout)

process.on('exit', function() {
	ffmpeg.kill();
});

module.exports = {
	output: ffmpeg.stdin
}
