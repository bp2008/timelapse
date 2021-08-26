<template>
	<div class="templateRoot">
		<div v-if="allCameras" class="allCameras">
			<AllPageCamera v-for="camera in allCameras" :key="camera.id" :camera="camera" />
		</div>
		<div v-else-if="error">An error occurred: {{error}}</div>
		<div v-else><ScaleLoader /></div>
	</div>
</template>

<script>
	import AllPageCamera from 'appRoot/vues/all/AllPageCamera.vue';
	export default {
		components: { AllPageCamera },
		props:
		{
		},
		data()
		{
			return {
				error: null
			};
		},
		computed:
		{
			allCameras()
			{
				return this.$store.state.allCameras;
			}
		},
		methods:
		{
		},
		created()
		{
			this.$store.dispatch("GetAllCameras").catch(err =>
			{
				this.error = err.message;
			});
		},
		mounted()
		{
		},
		beforeDestroy()
		{
		}
	};
</script>

<style scoped>
	.allCameras
	{
		display: flex;
		flex-wrap: wrap;
	}

		.allCameras > *
		{
			margin: 10px;
		}
</style>