import Vue from 'vue';
import Vuex from 'vuex';
Vue.use(Vuex);
import createPersistedState from 'vuex-persistedstate';

export default function CreateStore()
{
	return new Vuex.Store({
		strict: false, // Disable 'strict' for releases to improve performance
		plugins: [createPersistedState({ storage: window.localStorage })],
		state:
		{
		},
		getters:
		{
		},
		mutations: // mutations must not be async
		{
		},
		actions: // actions can be async
		{

		}
	});
}