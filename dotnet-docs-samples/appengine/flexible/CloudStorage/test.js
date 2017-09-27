﻿// Copyright(c) 2017 Google Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not
// use this file except in compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
// License for the specific language governing permissions and limitations under
// the License.

// 1.0 style test script not using the `casperjs test` subcommand
var casper = require('casper').create();
var host = casper.cli.args[0];

casper.start(host + '/', function (response) {
    console.log('Starting ' + host + '/');
    this.test.assertEquals(200, response.status);
    this.test.assertSelectorHasText('H1', 'Cloud Storage Sample');
    this.test.assertEquals("", this.fetchText('#content'));
    this.fill('#TextForm', {
        'content': 'sunshine',
    }, false);
    console.log('Filled form.');
});

casper.thenClick('#Submit', function (response) {
    console.log('Submitted form.');
    this.test.assertEquals(200, response.status);
    this.test.assertSelectorHasText('#content', 'sunshine');
    this.fill('#TextForm', {
        'content': 'rain',
    }, false);
    console.log('Filled form.');
});

casper.thenClick('#Submit', function (response) {
    console.log('Submitted form.');
    this.test.assertEquals(200, response.status);
    this.test.assertSelectorHasText('#content', 'rain');
});

casper.run(function () {
    this.test.done();
    this.test.renderResults(true);
});
