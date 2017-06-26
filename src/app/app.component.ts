import { Component, OnInit, ChangeDetectorRef } from '@angular/core';


declare var myExtObject: any;
@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],

})

export class AppComponent implements OnInit {

  id: string;
  name: string;


  constructor(private ref: ChangeDetectorRef) { }

  ngOnInit() {
    this.initializeMagic();
  }

  initializeMagic() {

    var self = this;
    myExtObject.startMagic(data => {
      var obj = JSON.parse(data);
      //alert(data);
      self.id = obj[1].Value;
      self.name = obj[3].Value;
      self.ref.detectChanges();
    }
    );
  }

  buttonClick(index: number) {
    myExtObject.buttonClick(index);
  }


}
