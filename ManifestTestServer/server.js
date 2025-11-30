const express = require('express');
const morgan = require('morgan');
const path = require('path');

const app = express();
const port = process.env.PORT || 3000;

app.use(morgan('dev'));

// thư mục chứa manifest mẫu
const manifestsDir = path.join(__dirname, 'manifests');

function sendJson(res, obj) {
  res.setHeader('Content-Type', 'application/json; charset=utf-8');
  res.send(JSON.stringify(obj, null, 2));
}

// manifest chuẩn (OK)
app.get('/manifest/latest.json', (req, res) => {
  const data = require(path.join(manifestsDir, 'manifest-1.2.0.0-ok.json'));
  sendJson(res, data);
});

// manifest bản 1.0.0.0 (silent)
app.get('/manifest/1.0.0.0.json', (req, res) => {
  const data = require(path.join(manifestsDir, 'manifest-1.0.0.0.json'));
  sendJson(res, data);
});

// manifest bản 1.1.0.0 (ask)
app.get('/manifest/1.1.0.0.json', (req, res) => {
  const data = require(path.join(manifestsDir, 'manifest-1.1.0.0.json'));
  sendJson(res, data);
});

// manifest lỗi JSON
app.get('/manifest/bad/latest.json', (req, res) => {
  res.setHeader('Content-Type', 'application/json; charset=utf-8');
  res.send('{"version": "broken", invalid-json-here');
});

// manifest lỗi server (500)
app.get('/manifest/error/latest.json', (req, res) => {
  res.status(500).json({ error: 'Internal test error from manifest server' });
});

// manifest slow (timeout test): delay 10s
app.get('/manifest/slow/latest.json', (req, res) => {
  setTimeout(() => {
    const data = require(path.join(manifestsDir, 'manifest-1.2.0.0-ok.json'));
    sendJson(res, data);
  }, 10000);
});


// manifest với MD5 mismatch
app.get('/manifest/md5-mismatch/latest.json', (req, res) => {
  const data = require(path.join(manifestsDir, 'manifest-1.2.0.0-md5-mismatch.json'));
  sendJson(res, data);
});

// manifest mirror (1 URL lỗi, 1 URL OK)
app.get('/manifest/mirror/latest.json', (req, res) => {
  const data = require(path.join(manifestsDir, 'manifest-1.2.0.0-mirror.json'));
  sendJson(res, data);
});

// manifest groups/tags
app.get('/manifest/groups/latest.json', (req, res) => {
  const data = require(path.join(manifestsDir, 'manifest-1.2.0.0-groups.json'));
  sendJson(res, data);
});

// file OK (MD5 đúng với manifest-1.2.0.0-ok, -mirror, -groups)
app.get('/files/ok/1.2.0.0/Dh.Updater.DebugConsole.Core.dll', (req, res) => {
  const buf = Buffer.from('OK-1.2.0.0-console', 'utf8');
  res.setHeader('Content-Type', 'application/octet-stream');
  res.send(buf);
});

// file BAD MD5 (manifest khai md5 khác)
app.get('/files/badmd5/1.2.0.0/Dh.Updater.DebugConsole.Core.dll', (req, res) => {
  const buf = Buffer.from('BAD-MD5-1.2.0.0', 'utf8');
  res.setHeader('Content-Type', 'application/octet-stream');
  res.send(buf);
});

// file mirror: URL bad -> 500
app.get('/files/mirror/bad/1.2.0.0/Dh.Updater.DebugConsole.Core.dll', (req, res) => {
  res.status(500).json({ error: 'Mirror BAD for test' });
});

// file mirror: URL good -> payload OK
app.get('/files/mirror/good/1.2.0.0/Dh.Updater.DebugConsole.Core.dll', (req, res) => {
  const buf = Buffer.from('OK-1.2.0.0-console', 'utf8');
  res.setHeader('Content-Type', 'application/octet-stream');
  res.send(buf);
});


// manifest partial download test (dùng chung manifest OK)
app.get('/manifest/partial/latest.json', (req, res) => {
  const data = require(path.join(manifestsDir, 'manifest-1.2.0.0-ok.json'));
  sendJson(res, data);
});

// file partial: gửi ít bytes rồi đóng kết nối
app.get('/files/partial/1.2.0.0/Dh.Updater.DebugConsole.Core.dll', (req, res) => {
  res.setHeader('Content-Type', 'application/octet-stream');
  res.write('PARTIAL-');
  // cố tình không end() hoặc res.destroy() nhanh, tuỳ Node sẽ đóng kết nối khi client timeout.
  res.destroy();
});

// file flaky: lần đầu lỗi, lần sau OK (trạng thái đơn giản trong memory)
let flakyHit = 0;
app.get('/files/flaky/1.2.0.0/Dh.Updater.DebugConsole.Core.dll', (req, res) => {
  flakyHit++;
  if (flakyHit === 1) {
    res.status(500).json({ error: 'Flaky first attempt' });
  } else {
    const buf = Buffer.from('OK-1.2.0.0-console', 'utf8');
    res.setHeader('Content-Type', 'application/octet-stream');
    res.send(buf);
  }
});


// v16: ZIP package test manifests

// static folder for zip/files
app.use('/static', express.static(path.join(__dirname, 'static')));
app.use('/files/zipfallback', express.static(path.join(__dirname, 'files_zipfallback')));

// ZIP OK manifest: ZIP có sha512 đúng, file_md5 map tới nội dung trong ZIP
app.get('/manifest/zip/ok/latest.json', (req, res) => {
  const manifest = {
  "version": "1.2.3.4",
  "sha512": "068f894311204e25d903d789be94b993929eaab782139023d39b44e963be4ac03ab47ca909063950d6169d49a08dcdf2ebd1849708341eebcd3257049c0d1548",
  "package_type": "zip_full",
  "zip_urls": [
    "/static/app-zip-ok-1.2.3.4.zip"
  ],
  "file_md5": {
    "MyApp.exe": {
      "md5": "7e118f5e546a5f2b8051cf9d6500a89b"
    },
    "Lib.dll": {
      "md5": "6e83e698cb1e06986eba47af393a1be5"
    }
  },
  "changelog": "Zip OK test manifest",
  "update_level": "optional"
};
  sendJson(res, manifest);
});

// ZIP fail manifest: zip_urls trỏ tới file không tồn tại, fallback về per-file URLs
app.get('/manifest/zip/fail-perfile/latest.json', (req, res) => {
  const manifest = {
  "version": "2.0.0.0",
  "sha512": "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000",
  "package_type": "zip_full",
  "zip_urls": [
    "/static/app-zip-bad-2.0.0.0.zip"
  ],
  "file_md5": {
    "MyApp.exe": {
      "md5": "45d1633869efb5800cde7897125b047d",
      "urls": [
        "/files/zipfallback/MyApp.exe"
      ]
    },
    "Lib.dll": {
      "md5": "263120424c055a44333d87fd074e15e8",
      "urls": [
        "/files/zipfallback/Lib.dll"
      ]
    }
  },
  "changelog": "Zip fail then per-file OK",
  "update_level": "optional"
};
  sendJson(res, manifest);
});

// ZIP sha512 ok nhưng MD5 một file sai, engine phải rollback ZIP và fallback per-file
app.get('/manifest/zip/bad-md5/latest.json', (req, res) => {
  const manifest = {
  "version": "3.0.0.0",
  "sha512": "068f894311204e25d903d789be94b993929eaab782139023d39b44e963be4ac03ab47ca909063950d6169d49a08dcdf2ebd1849708341eebcd3257049c0d1548",
  "package_type": "zip_full",
  "zip_urls": [
    "/static/app-zip-ok-1.2.3.4.zip"
  ],
  "file_md5": {
    "MyApp.exe": {
      "md5": "45d1633869efb5800cde7897125b047d",
      "urls": [
        "/files/zipfallback/MyApp.exe"
      ]
    },
    "Lib.dll": {
      "md5": "263120424c055a44333d87fd074e15e8",
      "urls": [
        "/files/zipfallback/Lib.dll"
      ]
    }
  },
  "changelog": "Zip OK sha512 nh\u01b0ng m\u1ed9t file MD5 mismatch => fallback per-file",
  "update_level": "optional"
};
  sendJson(res, manifest);
});

app.listen(port, () => {
  console.log(`Manifest test server listening at http://localhost:${port}`);
  console.log('Endpoints:');
  console.log('  /manifest/latest.json');
  console.log('  /manifest/bad/latest.json');
  console.log('  /manifest/error/latest.json');
  console.log('  /manifest/slow/latest.json');
});
