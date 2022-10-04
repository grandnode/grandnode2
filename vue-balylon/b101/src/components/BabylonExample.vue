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
    const canvas = this.$refs.canvas;
    new BasicScene(canvas);
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
