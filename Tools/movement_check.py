"""Actual native movement check; local test bindings C/LeftShift/Space."""
import os,json,time,hashlib
from pathlib import Path
from um.win import Drive
root=Path(__file__).resolve().parents[1]
b=Path(os.environ['LOCALAPPDATA'])/'UltraHaul/dev'
log=Path(os.environ['USERPROFILE'])/'AppData/LocalLow/semiwork/REPO/Player.log'
def state():return json.loads((b/'state.json').read_text())
def command(text,delay=.3):
    p=b/'command.txt';tmp=b/'command.tmp';tmp.write_text(text);tmp.replace(p)
    stop=time.monotonic()+5
    while p.exists() and time.monotonic()<stop:time.sleep(.02)
    assert not p.exists(),'Native game did not consume '+text
    time.sleep(delay)
d=Drive('REPO');e={'passed':False,'pluginSha256':hashlib.sha256((root/'bin/Release/netstandard2.1/UltraHaul.dll').read_bytes()).hexdigest()}
try:
    d.focus();d.cmd('scanmode on');start=len(log.read_text())
    assert state()['playing'] and not state()['godMode'],'Enter a fresh development solo run with god mode disabled.'
    command('movement point',1);command('measure');d.focus();d.key('0x43');time.sleep(.6)
    e['dash']=state();assert e['dash']['peakSpeed']>=20
    command('movement point',1);d.key('0x57','down');time.sleep(.2);d.cmd('hold 0xA0 350');d.key('0x57','up');time.sleep(.4)
    assert 'ACTION native slide' in log.read_text()[start:];e['slide']='native slide input logged'
    command('movement point',1);base=state()['position']['y'];d.cmd('hold 0x20 120');points=[]
    for i in range(15):points.append(state()['position']['y']);time.sleep(.1)
    e['jumpRise']=max(points)-base;assert e['jumpRise']>.5
    command('movement point',1);command('measure');command('slam drop',.1);d.cmd('hold 0xA0 250');time.sleep(1)
    e['slam']=state();assert e['slam']['minVertical']<=-24
    e['nativeKnockdownAfterSlam']=e['slam']['tumbling'];e['passed']=True
    print('PASS speeds/input:',e['dash']['peakSpeed'],'m/s dash;',e['jumpRise'],'m jump;',e['slam']['minVertical'],'m/s slam. Native knockdown:',e['nativeKnockdownAfterSlam'])
except Exception as error:
    e['error']=str(error)
    raise
finally:
    try:
        d.focus()
        for key in ('0x57','0xA0','0x20'):d.key(key,'up')
    finally:
        d.close()
        e['note']='Native C dash, W+Crouch slide and Space jump. Slam uses a real airborne controller drop with crouch input. No god mode. Native knockdowns remain; landing immunity is not claimed.'
        (root/'movement-results.json').write_text(json.dumps(e,indent=2)+'\n',encoding='utf-8')
