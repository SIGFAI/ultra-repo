"""Check the loaded plugin in the actual game. Requires ULTRAHAUL_DEV=1.
Run in a fresh development save, never a player's existing run.
"""
import atexit,hashlib,json,os,re,sys,time
from pathlib import Path
from um.win import Drive,shot

root=Path(__file__).resolve().parents[1]
bridge=Path(os.environ['LOCALAPPDATA'])/'UltraHaul'/'dev'
log=Path(os.environ['USERPROFILE'])/'AppData/LocalLow/semiwork/REPO/Player.log'
def state():
    try:return json.loads((bridge/'state.json').read_text())
    except (OSError,json.JSONDecodeError):return {}
def wait(predicate,seconds=20):
    stop=time.monotonic()+seconds
    while time.monotonic()<stop:
        s=state()
        if predicate(s):return s
        time.sleep(.05)
    raise AssertionError('Runtime state did not reach condition: '+str(state()))
def command(text,delay=.3):
    path=bridge/'command.txt';tmp=bridge/'command.tmp';tmp.write_text(text);tmp.replace(path)
    stop=time.monotonic()+5
    while path.exists() and time.monotonic()<stop:time.sleep(.02)
    assert not path.exists(),'Game did not consume '+text
    time.sleep(delay)
def god(enabled):
    if state().get('godMode')!=enabled:command('native godmode');wait(lambda s:s.get('godMode')==enabled)

d=Drive('REPO');d.focus();d.cmd('scanmode on')
assert state().get('assets'),'Wait for native ULTRAKILL assets to load.'
assert state().get('playing'),'Enter a fresh solo development run through the native menu before this check.'
time.sleep(.5)
start=len(log.read_text());evidence={}
def save_evidence():
    evidence['pluginSha256']=hashlib.sha256((root/'bin/Release/netstandard2.1/UltraHaul.dll').read_bytes()).hexdigest()
    evidence['cooperativeJoin']='pending two authenticated clients'
    evidence['log']='\n'.join(line for line in log.read_text()[start:].splitlines() if 'UltraHaul]' in line)
    name='control-results.json' if '--controls-only' in sys.argv else 'combat-results.json' if '--combat-only' in sys.argv else 'test-results.json'
    (root/name).write_text(json.dumps(evidence,indent=2)+'\n',encoding='utf-8')
atexit.register(save_evidence)
if '--controls-only' in sys.argv:
    try:
        assert not state().get('godMode'),'Disable god mode for the button check.'
        if state().get('equipped'):d.key('0x47');wait(lambda s:not s.get('equipped'),3)
        d.key('0x47');evidence['draw']=wait(lambda s:s.get('equipped'),3)['equipped']
        prior=state()['weapon'];d.key('0x52');evidence['switch']=wait(lambda s:s.get('weapon')!=prior,3)['weapon']
        d.key('0x46');time.sleep(.6)
        assert 'ACTION parry armed' in log.read_text()[start:];evidence['parryInput']=True
        if state()['weapon']!=0:d.key('0x52');wait(lambda s:s.get('weapon')==0,3)
        d.click(800,500,right=True);time.sleep(.2);d.click(800,500);time.sleep(.6)
        assert 'HOST fire revolver' in log.read_text()[start:];evidence['fireInput']=True
        d.key('0x47');evidence['holster']=not wait(lambda s:not s.get('equipped'),3)['equipped']
        evidence['note']='Actual G draw/holster, R swap, F parry and LMB fire, god mode disabled. RMB input sent; coin interaction is verified separately by the combat check. Natural AI attack timing is unverified.'
        evidence['passed']=True
        print('PASS: native G/R/F/LMB controls with god mode disabled; holstered for looting.')
    except Exception as error:
        evidence['error']=str(error)
        raise
    finally:
        try:d.focus();d.cmd('mup');d.cmd('mup right')
        finally:d.close()
    sys.exit(0)
god(True)
if '--combat-only' not in sys.argv:
    command('movement point',.3)
    command('measure')
    d.key('0x43');time.sleep(.6)
    s=state();assert s.get('peakSpeed',0)>=20,s;evidence['dash']=s
    command('movement point',.3);d.key('0x57','down');time.sleep(.2);d.cmd('hold 0xA0 400');d.key('0x57','up');time.sleep(.3)
    assert 'ACTION native slide' in log.read_text()[start:],'Native slide did not start.'
    command('movement point',1);wait(lambda s:s.get('grounded') and not s.get('sliding'),5);command('measure');command('slam drop',.1);d.cmd('hold 0xA0 250');time.sleep(.6)
    s=wait(lambda s:s.get('minVertical',0)<=-24,3);evidence['slam']=s
command('native spawn enemy hunter',6) # Native EnemyParent starts with a random 2–5 second despawn timer.
command('entities');assert re.search(r'ENEMY Huntsman active=True health=250',log.read_text()[start:]),'Native Huntsman has not activated.'
command('freeze enemies');god(False);command('parry test',1.2)
text=log.read_text()[start:]
timed=re.search(r'CHECK timed native incoming hit: health (\d+) -> (\d+) dash=([\d.]+)',text)
expired=re.search(r'CHECK expired window: health (\d+) -> (\d+)',text)
assert timed and timed[1]==timed[2],text
assert re.search(r'HIT .+ \d+ -> \d+ via parry',text) and 'STYLE parry' in text,'No native counter damage/style: '+text
assert expired and int(expired[2])<int(expired[1]),text
evidence['parry']='\n'.join(line for line in text.splitlines() if 'UltraHaul]' in line)
god(True)
command('revolver');command('aim enemy',.05);command('fire',.45)
command('aim enemy',.05);command('boost shot',1);command('aim enemy',.05);command('coin shot',1)
command('shotgun');time.sleep(1);command('aim enemy',.05);command('fire',1)
text=log.read_text()[start:]
for kind in ['revolver','coin','boost','shotgun']:
    assert re.search(r'HIT .+ \d+ -> \d+ via '+kind,text),'No real enemy hit via '+kind
evidence['combat']='\n'.join(line for line in text.splitlines() if 'UltraHaul]' in line)
evidence['stateAfterCombat']=state();assert evidence['stateAfterCombat'].get('style',0)>0,'No style awarded for actual combat'
god(False)
evidence['note']='Actual native game, enemies and loot. God mode isolates weapon/movement checks; disabled for timed native Hurt/parry check. Slam uses a real controller drop with crouch input, independently of jump setup. Native incoming hit is scripted; AI attack timing is a separate playtest. Combat positioned close to native Huntsman; boost input in firing frame. Rapid revolver-to-shotgun switch checked.'
evidence['pluginSha256']=hashlib.sha256((root/'bin/Release/netstandard2.1/UltraHaul.dll').read_bytes()).hexdigest()
evidence['cooperativeJoin']='pending two authenticated clients'
evidence['passed']=True
print('PASS: '+('enemy combat only' if '--combat-only' in sys.argv else 'real dash/slide/slam and enemy combat')+', timed native parry and expired window. Co-op remains pending.')

