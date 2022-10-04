<template>
  <div>
    <h3>Babylon Examples</h3>
    <canvas ref="canvas"></canvas>
  </div>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
 import {BasicScene} from '@/BabylonExamples/BasicScene';


export default defineComponent({
  name: 'BabylonExamples',
  mounted(){
    console.log(this.$refs.canvas);
    new BasicScene(this.$refs.canvas as HTMLCanvasElement);
  }
});

</script>

<!-- Add "scoped" attribute to limit CSS to this component only -->
<style scoped>
canvas{
  width: 70%;
  height: 70%;
}
</style>
