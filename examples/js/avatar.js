window.ntExample = {
  randomColor: () => {
    return '#' + new THREE.Color(Math.random(), Math.random(), Math.random()).getHexString();
  }
};

// Temporary workaround for template declaration; see issue 167
NAF.schemas.getComponentsOriginal = NAF.schemas.getComponents;
NAF.schemas.getComponents = (template) => {
  if (!NAF.schemas.hasTemplate('#head-template')) {
    NAF.schemas.add({
      template: '#head-template',
      components: [
        'position',
        'rotation',

        // In this example, we don't sync the material.color itself, like the basic example;
        // we instead sync player-info, which includes color setting + updating.
        // (you can see an example of the other pattern in the basic.html demo)
        'player-info'
      ]
    });
  }

  const components = NAF.schemas.getComponentsOriginal(template);
  return components;
};

AFRAME.registerComponent('player-info', {
  // notice that color and name are both listed in the schema; NAF will only keep
  // properties declared in the schema in sync.
  schema: {
    name: { type: 'string', default: 'user-' + Math.round(Math.random() * 10000) },
    color: {
      type: 'color', // btw: color is just a string under the hood in A-Frame
      default: window.ntExample.randomColor()
    }
  },

  init: function () {
    this.head = this.el.querySelector('.head');
    this.nametag = this.el.querySelector('.nametag');

    this.ownedByLocalUser = this.el.id === 'player';
    if (this.ownedByLocalUser) {
      // populate the html overlay with the correct name on init
      this.nametagInput = document.getElementById('username-overlay');
      this.nametagInput.value = this.data.name;

      // add the initial color to the html overlay color picker button
      document.querySelector('#color-changer').style.backgroundColor = this.data.color;
      document.querySelector('#color-changer').style.color = this.data.color;
    }
  },

  // here as an example, not used in current demo. Could build a user list, expanding on this.
  listUsers: function () {
    console.log(
      'userlist',
      [...document.querySelectorAll('[player-info]')].map((el) => el.components['player-info'].data.name)
    );
  },

  newRandomColor: function () {
    this.el.setAttribute('player-info', 'color', window.ntExample.randomColor());
  },

  update: function () {
    if (this.head) this.head.setAttribute('material', 'color', this.data.color);
    if (this.nametag) this.nametag.setAttribute('value', this.data.name);
  }
});
// Mic status
let micEnabled = true;
// Mic button element
const micBtnEle = document.getElementById('mic-btn');

// Called by Networked-Aframe when connected to server
function onConnect() {
  console.log('onConnect', new Date());

  // Handle mic button click (Mute and Unmute)
  micBtnEle.addEventListener('click', function () {
    NAF.connection.adapter.enableMicrophone(!micEnabled);
    micEnabled = !micEnabled;
    micBtnEle.textContent = micEnabled ? 'Mute Mic' : 'Unmute Mic';
  });
}