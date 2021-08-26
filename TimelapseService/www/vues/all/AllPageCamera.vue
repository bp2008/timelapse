<template>
	<div class="allPageCamera">
		<a :href="imageLink" class="imgLink"><img :src="imageUrl" /></a>
		<a :href="nameLink" class="nameLink">{{camera.name}}</a>
	</div>
</template>

<script>
	export default {
		components: {},
		props:
		{
			camera: {
				type: Object,
				required: true
			}
		},
		data()
		{
			return {
			};
		},
		computed:
		{
			imageUrl()
			{
				if (this.camera.imageUrl)
					return this.camera.imageUrl;
				else
					return appContext.appPath + "TimelapseAPI/Image/" + this.camera.id + "/latest.jpg";
			},
			imageLink()
			{
				if (this.camera.imageLink)
					return this.camera.imageLink;
				else
					return appContext.appPath + "TimelapseAPI/Image/" + this.camera.id + "/latest.jpg";
			},
			nameLink()
			{
				if (this.camera.nameLink)
					return this.camera.nameLink;
				else
					return appContext.appPath + "TimelapseAPI/Image/" + this.camera.id + "/latest.jpg";
			}
		},
		methods:
		{
		},
		created()
		{
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
	.allPageCamera
	{
		display: inline-block;
		width: 320px;
	}

		.allPageCamera img
		{
			max-width: 320px;
			max-height: 240px;
		}

	.nameLink
	{
		display: block;
		text-align: center;
	}
</style>