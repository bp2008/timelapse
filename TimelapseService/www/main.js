import Vue from 'vue';
import App from 'appRoot/vues/App.vue';
import CreateStore from 'appRoot/store/store.js';
import CreateRouter from 'appRoot/router/index.js';

import '@deveodk/vue-toastr/dist/@deveodk/vue-toastr.css';
import VueToastr from '@deveodk/vue-toastr';
Vue.use(VueToastr);

import ScaleLoader from 'appRoot/vues/common/ScaleLoader.vue';
Vue.component('ScaleLoader', ScaleLoader);

// Any recursively nested components must be globally registered here
//Vue.component('Example', require('Example.vue').default);

let store = window.store = CreateStore();
let router = window.router = CreateRouter(store, appContext.appPath);
let myApp = window.myApp = new Vue({
	store,
	router,
	...App
});

import ToasterHelper from 'appRoot/scripts/ToasterHelper.js';
window.toaster = new ToasterHelper(myApp.$toastr);

import * as Util from 'appRoot/scripts/Util.js';
window.Util = Util;

router.onReady(() =>
{
	const matchedComponents = router.getMatchedComponents();

	if (matchedComponents.length < 1)
		window.location.replace(appContext.appPath + "404.html");

	myApp.$mount('#app');
});