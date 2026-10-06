"""One check for the generator: reject missing cells, foreign keys and NaN."""
import json,shutil,subprocess,sys,tempfile
from pathlib import Path
source=Path(__file__).resolve().parents[1]
with tempfile.TemporaryDirectory() as tmp:
    root=Path(tmp)/'mod';(root/'Tools').mkdir(parents=True)
    shutil.copytree(source/'Sheets',root/'Sheets')
    shutil.copy(source/'Tools'/'preflight.py',root/'Tools')
    (root.parent/'reference').mkdir()
    for row in json.loads((root/'Sheets'/'hooks.json').read_text())['rows']:
        shutil.copy(source.parent/'reference'/row['source'],root.parent/'reference'/row['source'])
    def run(ok):
        result=subprocess.run([sys.executable,str(root/'Tools'/'preflight.py')],capture_output=True,text=True)
        assert (result.returncode==0)==ok,result.stdout+result.stderr
    run(True)
    for sheet,column,value in [('movement','walk',None),('weapons','asset','missing'),('movement','walk',float('nan'))]:
        path=root/'Sheets'/f'{sheet}.json';original=path.read_text();data=json.loads(original)
        data['rows'][0][column]=value;path.write_text(json.dumps(data));run(False);path.write_text(original)
    run(True)
print('PASS: complete sheets generate; incomplete, unresolved and nonfinite cells fail.')
