require File.dirname(__FILE__) + '/../../spec_helper'
require 'syslog'

describe "Syslog.inspect" do
  not_supported_on :windows do

    before :each do
      Syslog.opened?.should be_false
    end

    after :each do
      Syslog.opened?.should be_false
    end

    it "returns a string a closed log" do
      Syslog.inspect.should == "<#Syslog: opened=false>"
    end

    it "returns a string for an opened log" do
      Syslog.open
      Syslog.inspect.should =~ /<#Syslog: opened=true.*/
      Syslog.close
    end

    it "includes the ident, options, facility and mask" do
      Syslog.open("rubyspec", Syslog::LOG_PID, Syslog::LOG_USER)
      inspect_str = Syslog.inspect.split ", "
      inspect_str[0].should == "<#Syslog: opened=true"
      inspect_str[1].should == "ident=\"rubyspec\""
      inspect_str[2].should == "options=#{Syslog::LOG_PID}"
      inspect_str[3].should == "facility=#{Syslog::LOG_USER}"
      inspect_str[4].should == "mask=255>"
      Syslog.close
    end
  end
end
