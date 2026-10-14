import importlib.util, tempfile, pathlib, unittest
from unittest.mock import patch
spec=importlib.util.spec_from_file_location('archive', str(pathlib.Path(__file__).resolve().parents[1] / 'archive-nightly.py'))
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
class Tests(unittest.TestCase):
 def run_case(self, corrupt=False, same=False):
  calls=[]; data=b'old package'; asset={'id':1,'name':'old.zip','size':len(data),'updated_at':'2026-10-14T15:00:00Z'}
  release={'tag_name':'Nightly','assets':[asset],'body':'old notes'}
  def gh(*a):
   calls.append(a)
   if a[0]=='api':return __import__('json').dumps([[release]]).encode()
   if a[:2]==('release','download'):
    dest=pathlib.Path(a[a.index('--dir')+1]);(dest/'old.zip').write_bytes(b'bad' if corrupt and a[2]!='Nightly' else data)
   return b''
  with tempfile.TemporaryDirectory() as d,patch.dict(m.os.environ, {'SOURCE_SHA':'publishing-head'}),patch.object(m,'gh',gh),patch.object(m,'api',return_value={'object':{'type':'commit','sha':'original'}}):
   incoming=pathlib.Path(d);(incoming/('old.zip' if same else 'new.zip')).write_bytes(data if same else b'new')
   if corrupt:
    with self.assertRaises(ValueError):m.archive('owner/repo',incoming)
   else:m.archive('owner/repo',incoming)
  return calls
 def test_verified_archive(self):
  calls=self.run_case();create=next(x for x in calls if x[:2]==('release','create'));self.assertEqual(create[create.index('--target')+1],'publishing-head');self.assertEqual(sum(x[:2]==('release','download') for x in calls),2);self.assertTrue(any(x[:2]==('release','edit') for x in calls))
 def test_failure_does_not_publish_archive(self):self.assertFalse(any(x[:2]==('release','edit') for x in self.run_case(corrupt=True)))
 def test_identical_republish(self):self.assertFalse(any(x[:2]==('release','create') for x in self.run_case(same=True)))
unittest.main()
