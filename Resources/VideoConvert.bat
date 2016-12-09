


::for %%A IN ("Videos/*.mp4") DO ffmpeg -i "Videos/%%A" -c:v libtheora -c:a libvorbis -q:v 6 -q:a 5 "Videos/%%A.ogg"
for %%A IN ("Videos/*.mp4") DO ffmpeg -i "Videos/%%A"  -acodec libtheora -c:a libvorbis -q:v 6 -q:a 5 -ss 00:00:30 -t 00:01:00 "Videos/%%A.ogg"
for %%A IN ("Videos/*.avi") DO ffmpeg -i "Videos/%%A"  -acodec libtheora -c:a libvorbis -q:v 6 -q:a 5 -ss 00:00:30 -t 00:01:00 "Videos/%%A.ogg"








:: OLD
:: for %%A IN ("Videos/*.mp4") DO ffmpeg -i "Videos/%%A" -acodec libvorbis "Videos/%%A.ogg"
:: ffmpeg -i input.mkv -codec:v libtheora -qscale:v 7 -codec:a libvorbis -qscale:a 5 output.ogv
:: for %%A IN ("Videos/*.mp4") DO ffmpeg -i "Videos/%%A" -acodec libvorbis "Videos/%%A.ogg"

:: ffmpeg2theora: http://v2v.cc/~j/ffmpeg2theora/download.html
:: Documentation: http://v2v.cc/~j/ffmpeg2theora/examples.html
:: for %%A IN ("Videos/*.mp4") DO ffmpeg2theora "Videos/%%A"