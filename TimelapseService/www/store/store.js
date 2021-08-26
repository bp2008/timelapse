import Vue from 'vue';
import Vuex from 'vuex';
Vue.use(Vuex);
//import createPersistedState from 'vuex-persistedstate';

import { GetJson } from 'appRoot/api/api.js';

export default function CreateStore()
{
	return new Vuex.Store({
		strict: false, // Disable 'strict' for releases to improve performance
		//plugins: [createPersistedState({ storage: window.localStorage })],
		state:
		{
			allCameras: null
		},
		getters:
		{
		},
		mutations: // mutations must not be async
		{
			SetAllCameras(state, allCameras)
			{
				state.allCameras = allCameras;
			}
		},
		actions: // actions can be async
		{
			GetAllCameras(store)
			{
				if (store.state.allCameras)
					return Promise.resolve(store.state.allCameras);
				return GetJson("AllCameras").then(allCameras =>
				{
					store.commit("SetAllCameras", allCameras);
					return Promise.resolve(store.state.allCameras);
				});
			}
		}
	});
}