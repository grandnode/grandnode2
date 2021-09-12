(function (global, factory) {
  typeof exports === 'object' && typeof module !== 'undefined' ? module.exports = factory() :
  typeof define === 'function' && define.amd ? define(factory) :
  (global.poper = factory());
}(this, (function () { 'use strict';

var index = function (input, data, ref) {
  if ( data === void 0 ) data = {};
  if ( ref === void 0 ) ref = {};
  var stringify = ref.stringify; if ( stringify === void 0 ) stringify = false;

  if (typeof input !== 'string') {
    throw new Error('Expected input to be string')
  }

  //          |        match start       | match content   |         match end        |
  var re = /\/\*\s*@@([\w.\-_]+)\s*\*\/\s*(?:[\s\S]*?)\s*\/\*\s*([\w.\-_]+)@@\s*\*\//g;

  return input.replace(re, function (match, key, endKey) {
    if (key === endKey) {
      var ret = data;

      for (var i = 0, list = key.split('.'); i < list.length; i += 1) {
        var prop = list[i];

        ret = ret ? ret[prop] : '';
      }

      return stringify ? JSON.stringify(ret) : ret
    }

    return match
  })
};

return index;

})));
