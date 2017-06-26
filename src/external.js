var myExtObject = (function (cb) {

  return {
    func1: function (cb) {
      alert('function 1 called');

      var call = "Async call (Divide 16 / 2): " + Date();
    },
    func2: function (cb) {
      alert('function 2 called');
    },
    func3: function (cb) {
      alert('function 3 called');
      bound1.testCallback(cb);
    },
    startMagic: function (cb) {
      // alert('function 4 called');
      func = cb;
      magic1.start(cb);
    },
    buttonClick: function (idx) {
      magic1.insertEvent(idx);
    }
  }

})(myExtObject || {})

var func;

function simpleCallback(s) {
  alert(s);
}

function callback(s) {
  func(s);

}

function testCallback() {
  // bound.testCallback(callback);

  // var result = document.getElementById('cbresult');
  // result.innerText = "The function has returned: " + Date() + "\n";

  // window.location.assign("http://www.google.com");
}
