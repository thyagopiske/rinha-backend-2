import http from "k6/http";
import { sleep } from "k6";

export const options = {
  vus: 3,
  iterations: 3,
};

export default function () {
  for (let counter = 0; counter < 20; counter++) {
    // http.get(
    //   `http://localhost:5083/cuncurrencyTest/?counter=${counter}&VU=${__VU}`
    // );
    http.get(
      `http://localhost:5083/insertChannel`
    );
    console.log(
      "VU: " +
        __VU +
        " ITERATION: " +
        __ITER +
        " - " +
        new Date().toLocaleString()
    );
  }	
}
